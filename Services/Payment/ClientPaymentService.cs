using System.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Tech_Store.Events;
using Tech_Store.Extensions;
using Tech_Store.Models;
using Tech_Store.Models.DTO;
using Tech_Store.Models.DTO.Payment.Client;
using Tech_Store.Models.Enums;
using Tech_Store.Models.ViewModel;
using Tech_Store.Services.Admin.NotificationServices;
using Tech_Store.Services.Recommendation;
using System.Text.Json;

namespace Tech_Store.Services.Payment
{
    public class ClientPaymentService : IClientPaymentService
    {
        private const string PaymentInfoSessionKey = "PaymentInfo";
        private const string CartItemsSessionKey = "CartItems";
        private const int DefaultShippingAmount = 30000;

        private readonly ApplicationDbContext _context;
        private readonly IOnlinePaymentGatewayService _onlinePaymentGatewayService;
        private readonly NotificationService _notificationService;
        private readonly IPaymentGatewaySettingsService _paymentGatewaySettingsService;
        private readonly IUserProductEventTrackingService _userProductEventTrackingService;

        public ClientPaymentService(
            ApplicationDbContext context,
            IOnlinePaymentGatewayService onlinePaymentGatewayService,
            NotificationService notificationService,
            IPaymentGatewaySettingsService paymentGatewaySettingsService,
            IUserProductEventTrackingService userProductEventTrackingService)
        {
            _context = context;
            _onlinePaymentGatewayService = onlinePaymentGatewayService;
            _notificationService = notificationService;
            _paymentGatewaySettingsService = paymentGatewaySettingsService;
            _userProductEventTrackingService = userProductEventTrackingService;
        }

        public async Task<CheckoutPageResult> BuildCheckoutPageAsync(int userId, string? cartItemsJson, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(cartItemsJson))
            {
                return new CheckoutPageResult
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    ErrorMessage = "Không có sản phẩm nào trong giỏ hàng."
                };
            }

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
            if (user == null)
            {
                return new CheckoutPageResult
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    ErrorMessage = "Không tìm thấy người dùng."
                };
            }

            var address = await _context.Addresses.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
            var formattedAddress = await BuildFormattedAddressAsync(address, cancellationToken);
            var cartItems = JsonConvert.DeserializeObject<ListItemCartDTo>(cartItemsJson);

            if (cartItems?.cartDTos == null || cartItems.cartDTos.Count == 0)
            {
                return new CheckoutPageResult
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    ErrorMessage = "Không có sản phẩm nào trong giỏ hàng."
                };
            }

            var productDetails = await GetProductDetailsAsync(cartItems.cartDTos, cancellationToken);
            var availableOnlineGateways = (await _paymentGatewaySettingsService.GetGatewaySettingsAsync(cancellationToken))
                .Where(x => x.IsEnabled)
                .ToList();
            var subtotalAmount = productDetails.Sum(x => x.SellPrice * x.Quantity);
            var shippingAmount = productDetails.Count > 0 ? DefaultShippingAmount : 0;

            var model = new CheckoutPageViewModel
            {
                Products = productDetails,
                SubtotalAmount = subtotalAmount,
                ShippingAmount = shippingAmount,
                Customer = new UserDTo
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    AddressLine = address?.AddressLine,
                    Ward = address?.Ward,
                    Province = address?.Province,
                    District = address?.District
                },
                AvailableOnlineGateways = availableOnlineGateways
            };

            return new CheckoutPageResult
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                FormattedAddress = formattedAddress,
                Model = model
            };
        }

        public async Task<PaymentMethodValidationResult> ValidatePaymentMethodAsync(string? paymentMethod, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                return new PaymentMethodValidationResult
                {
                    Success = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    ErrorMessage = "Vui lòng chọn phương thức thanh toán."
                };
            }

            if (!PaymentMethodTypeExtensions.TryParseCode(paymentMethod, out var paymentMethodType))
            {
                return new PaymentMethodValidationResult
                {
                    Success = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    ErrorMessage = "Phương thức thanh toán không hợp lệ."
                };
            }

            if (paymentMethodType.IsOnlineGateway())
            {
                var isEnabled = await _paymentGatewaySettingsService.IsGatewayEnabledAsync(paymentMethodType.ToCode(), cancellationToken);
                if (!isEnabled)
                {
                    return new PaymentMethodValidationResult
                    {
                        Success = false,
                        StatusCode = StatusCodes.Status409Conflict,
                        ErrorMessage = "Cổng thanh toán này hiện đang tạm đóng. Vui lòng chọn phương thức khác."
                    };
                }
            }

            return new PaymentMethodValidationResult
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                PaymentMethodType = paymentMethodType
            };
        }

        public async Task<PaymentStartResult> CreateOnlinePaymentAsync(PaymentMethodType paymentMethodType, PaymentDTo model, int userId, HttpContext httpContext, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
            if (user == null)
            {
                return new PaymentStartResult
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    ErrorMessage = "Không tìm thấy người dùng."
                };
            }

            try
            {
                await BuildCheckoutContextAsync(model, cancellationToken);
                await GetValidatedVoucherAsync(model.VoucherCode, null, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return new PaymentStartResult
                {
                    Success = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    ErrorMessage = ex.Message
                };
            }

            var totalAmount = await GetTotalPriceAsync(model, cancellationToken);
            httpContext.Session.SetObject(PaymentInfoSessionKey, model);

            var gatewayResult = await _onlinePaymentGatewayService.CreatePaymentUrlAsync(
                paymentMethodType,
                new OnlinePaymentGatewayRequest
                {
                    Amount = totalAmount,
                    CreatedDate = DateTime.Now,
                    Description = "Đơn hàng",
                    FullName = $"{user.LastName} {user.FirstName}".Trim(),
                    OrderId = Random.Shared.Next(1000, 10000)
                },
                httpContext,
                cancellationToken);

            return new PaymentStartResult
            {
                Success = gatewayResult.Success,
                StatusCode = gatewayResult.StatusCode,
                RedirectUrl = gatewayResult.RedirectUrl,
                ErrorMessage = gatewayResult.ErrorMessage
            };
        }

        public async Task<PaymentStartResult> CreateCodOrderAsync(PaymentDTo model, int userId, HttpContext httpContext, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users.FindAsync([userId], cancellationToken);
            if (user == null)
            {
                return new PaymentStartResult
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    ErrorMessage = "Không tìm thấy người dùng."
                };
            }

            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var address = await ResolveShippingAddressAsync(model, userId, cancellationToken);
                if (address == null)
                {
                    throw new InvalidOperationException("Không tìm thấy địa chỉ vận chuyển.");
                }

                var checkoutContext = await BuildCheckoutContextAsync(model, cancellationToken);
                var order = await CreateOrderAggregateAsync(
                    model,
                    userId,
                    address.AddressId,
                    PaymentMethodType.Cod,
                    OrderStatusType.Pending,
                    PaymentRecordStatusType.Unpaid,
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                httpContext.Session.Remove(CartItemsSessionKey);
                await TrackPurchaseEventsAsync(userId, order.OrderId, PaymentMethodType.Cod, checkoutContext.Lines, cancellationToken);
                await NotifyOrderCreatedAsync(user, order.OrderId);

                return new PaymentStartResult
                {
                    Success = true,
                    StatusCode = StatusCodes.Status200OK,
                    RedirectUrl = "/Payment/OrderSuccess"
                };
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new PaymentStartResult
                {
                    Success = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    ErrorMessage = ex.Message
                };
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                return new PaymentStartResult
                {
                    Success = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    ErrorMessage = "Có lỗi xảy ra trong quá trình xử lý đơn hàng."
                };
            }
        }

        public async Task<PaymentCallbackResult> HandleMomoCallbackAsync(int userId, HttpContext httpContext, CancellationToken cancellationToken = default)
        {
            var callbackResult = _onlinePaymentGatewayService.ValidateCallback(PaymentMethodType.Momo, httpContext);
            if (!callbackResult.Success)
            {
                return new PaymentCallbackResult
                {
                    Success = false,
                    StatusCode = StatusCodes.Status200OK,
                    IsGatewayFailure = true,
                    GatewayFailureMessage = callbackResult.FailureMessage
                };
            }

            return await HandleSuccessfulGatewayPaymentAsync(
                userId,
                PaymentMethodType.Momo,
                httpContext,
                cancellationToken);
        }

        public async Task<PaymentCallbackResult> HandleVnPayCallbackAsync(int userId, HttpContext httpContext, CancellationToken cancellationToken = default)
        {
            var callbackResult = _onlinePaymentGatewayService.ValidateCallback(PaymentMethodType.VnPay, httpContext);
            if (!callbackResult.Success)
            {
                return new PaymentCallbackResult
                {
                    Success = false,
                    StatusCode = StatusCodes.Status200OK,
                    IsGatewayFailure = true,
                    GatewayFailureMessage = callbackResult.FailureMessage
                };
            }

            return await HandleSuccessfulGatewayPaymentAsync(
                userId,
                PaymentMethodType.VnPay,
                httpContext,
                cancellationToken);
        }

        public async Task<VoucherCheckResult> CheckVoucherAsync(string code, CancellationToken cancellationToken = default)
        {
            var normalizedCode = code?.ToLower().Trim() ?? string.Empty;
            var voucher = await _context.Vouchers.FirstOrDefaultAsync(x => x.Code.ToLower().Trim() == normalizedCode, cancellationToken);

            if (voucher == null)
            {
                return new VoucherCheckResult
                {
                    Success = false,
                    Message = "Voucher không đúng"
                };
            }

            var validationMessage = ValidateVoucher(voucher);
            if (!string.IsNullOrEmpty(validationMessage))
            {
                return new VoucherCheckResult
                {
                    Success = false,
                    Message = validationMessage
                };
            }

            return new VoucherCheckResult
            {
                Success = true,
                Message = "Voucher đã được áp dụng",
                Voucher = voucher
            };
        }

        private async Task<PaymentCallbackResult> HandleSuccessfulGatewayPaymentAsync(int userId, PaymentMethodType paymentMethodType, HttpContext httpContext, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FindAsync([userId], cancellationToken);
            if (user == null)
            {
                return new PaymentCallbackResult
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    ErrorMessage = "Không tìm thấy người dùng."
                };
            }

            var paymentInfo = httpContext.Session.GetObject<PaymentDTo>(PaymentInfoSessionKey);
            if (paymentInfo == null)
            {
                return new PaymentCallbackResult
                {
                    Success = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    ErrorMessage = "Không tìm thấy thông tin thanh toán."
                };
            }

            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var address = await ResolveShippingAddressAsync(paymentInfo, userId, cancellationToken);
                if (address == null)
                {
                    throw new InvalidOperationException("Không tìm thấy địa chỉ vận chuyển.");
                }

                var checkoutContext = await BuildCheckoutContextAsync(paymentInfo, cancellationToken);
                var order = await CreateOrderAggregateAsync(
                    paymentInfo,
                    userId,
                    address.AddressId,
                    paymentMethodType,
                    OrderStatusType.Confirmed,
                    PaymentRecordStatusType.Paid,
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                httpContext.Session.Remove(PaymentInfoSessionKey);
                httpContext.Session.Remove(CartItemsSessionKey);

                await TrackPurchaseEventsAsync(userId, order.OrderId, paymentMethodType, checkoutContext.Lines, cancellationToken);
                await NotifyGatewayPaymentSuccessAsync(user, order.OrderId);

                return new PaymentCallbackResult
                {
                    Success = true,
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new PaymentCallbackResult
                {
                    Success = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    ErrorMessage = ex.Message
                };
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                return new PaymentCallbackResult
                {
                    Success = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    ErrorMessage = "Có lỗi xảy ra trong quá trình xử lý đơn hàng."
                };
            }
        }

        private async Task<Address?> ResolveShippingAddressAsync(PaymentDTo paymentDTo, int userId, CancellationToken cancellationToken)
        {
            if (paymentDTo.NewAddress)
            {
                return await AddShipAddressAsync(paymentDTo, userId, cancellationToken);
            }

            return await _context.Addresses.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        }

        private async Task<Order> CreateOrderAggregateAsync(
            PaymentDTo paymentInfo,
            int userId,
            int addressId,
            PaymentMethodType paymentMethodType,
            OrderStatusType orderStatusType,
            PaymentRecordStatusType paymentRecordStatusType,
            CancellationToken cancellationToken)
        {
            var checkoutContext = await BuildCheckoutContextAsync(paymentInfo, cancellationToken);
            var originAmount = checkoutContext.Lines.Sum(x => x.UnitPrice * x.Quantity);
            var voucher = await GetValidatedVoucherAsync(paymentInfo.VoucherCode, originAmount, cancellationToken);
            var discountAmount = voucher == null ? 0 : ParseCurrency(voucher.Promotion, originAmount);
            var shippingAmount = GetShippingAmount(checkoutContext.Lines.Count);
            var totalAmount = Math.Max(0, originAmount - discountAmount + shippingAmount);

            ApplyInventoryDeduction(checkoutContext.Lines);

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                TotalAmount = totalAmount,
                OriginAmount = originAmount,
                Note = paymentInfo.Note,
                DiscountAmount = discountAmount,
                OrderStatus = orderStatusType.ToRecordValue(),
                ShippingAmount = shippingAmount,
                ShippingAddressId = addressId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(cancellationToken);

            var orderItems = checkoutContext.Lines.Select(line => new OrderItem
            {
                OrderId = order.OrderId,
                VarientProductId = line.Variant.VarientId,
                ProductId = line.Product.ProductId,
                Quantity = line.Quantity,
                Price = line.UnitPrice,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            }).ToList();

            _context.OrderItems.AddRange(orderItems);
            _context.Payments.Add(new Models.Payment
            {
                OrderId = order.OrderId,
                PaymentDate = DateTime.Now,
                PaymentMethod = paymentMethodType.ToPaymentRecordValue(),
                Status = paymentRecordStatusType.ToRecordValue(),
                Amount = totalAmount,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });

            AddInventoryTransactions(order.OrderId, userId, checkoutContext.Lines);
            ConsumeVoucher(voucher);
            await RemovePurchasedCartItemsAsync(userId, checkoutContext.Lines, cancellationToken);

            foreach (var product in checkoutContext.Lines.Select(x => x.Product).DistinctBy(x => x.ProductId))
            {
                product.Status = ResolveStockStatus(product.Stock, product.Status);
                product.UpdatedAt = DateTime.Now;
            }

            foreach (var variant in checkoutContext.Lines.Select(x => x.Variant).DistinctBy(x => x.VarientId))
            {
                variant.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return order;
        }

        private async Task<CheckoutContext> BuildCheckoutContextAsync(PaymentDTo paymentInfo, CancellationToken cancellationToken)
        {
            if (paymentInfo.Products == null || paymentInfo.Products.Count == 0)
            {
                throw new InvalidOperationException("Không có sản phẩm để tạo đơn hàng.");
            }

            var groupedItems = paymentInfo.Products
                .GroupBy(x => new { x.VarientProductID, x.ProductId })
                .Select(x => new CheckoutRequestLine
                {
                    VariantId = x.Key.VarientProductID,
                    ProductId = x.Key.ProductId,
                    Quantity = x.Sum(v => v.Quantity)
                })
                .ToList();

            if (groupedItems.Any(x => x.Quantity <= 0))
            {
                throw new InvalidOperationException("Số lượng sản phẩm không hợp lệ.");
            }

            var variantIds = groupedItems.Select(x => x.VariantId).Distinct().ToList();
            var variants = await _context.VarientProducts
                .Include(x => x.Product)
                .Where(x => variantIds.Contains(x.VarientId))
                .ToDictionaryAsync(x => x.VarientId, cancellationToken);

            if (variants.Count != variantIds.Count)
            {
                throw new InvalidOperationException("Một hoặc nhiều biến thể không còn tồn tại.");
            }

            var lines = new List<CheckoutLine>();

            foreach (var item in groupedItems)
            {
                var variant = variants[item.VariantId];
                var product = variant.Product ?? throw new InvalidOperationException("Không tìm thấy sản phẩm của biến thể.");

                if (variant.ProductId != item.ProductId)
                {
                    throw new InvalidOperationException("Dữ liệu sản phẩm không hợp lệ.");
                }

                var unitPrice = variant.Price ?? product.SellPrice;
                if (unitPrice == null || unitPrice <= 0)
                {
                    throw new InvalidOperationException($"Biến thể {variant.Sku} chưa có giá bán hợp lệ.");
                }

                var variantStock = variant.Stock ?? 0;
                if (variantStock < item.Quantity)
                {
                    throw new InvalidOperationException($"Tồn kho của biến thể {variant.Sku} không đủ.");
                }

                lines.Add(new CheckoutLine
                {
                    Product = product,
                    Variant = variant,
                    Quantity = item.Quantity,
                    UnitPrice = unitPrice.Value
                });
            }

            foreach (var productGroup in lines.GroupBy(x => x.Product.ProductId))
            {
                var product = productGroup.First().Product;
                var requiredQuantity = productGroup.Sum(x => x.Quantity);
                if (product.Stock < requiredQuantity)
                {
                    throw new InvalidOperationException($"Tồn kho của sản phẩm {product.Name} không đủ.");
                }
            }

            return new CheckoutContext
            {
                Lines = lines
            };
        }

        private void ApplyInventoryDeduction(IEnumerable<CheckoutLine> lines)
        {
            foreach (var productGroup in lines.GroupBy(x => x.Product.ProductId))
            {
                var product = productGroup.First().Product;

                foreach (var line in productGroup)
                {
                    var variantStock = line.Variant.Stock ?? 0;
                    if (variantStock < line.Quantity)
                    {
                        throw new InvalidOperationException($"Tồn kho của biến thể {line.Variant.Sku} không đủ.");
                    }

                    if (product.Stock < line.Quantity)
                    {
                        throw new InvalidOperationException($"Tồn kho của sản phẩm {product.Name} không đủ.");
                    }

                    line.Variant.Stock = variantStock - line.Quantity;
                    product.Stock -= line.Quantity;
                }
            }
        }

        private void AddInventoryTransactions(int orderId, int userId, IEnumerable<CheckoutLine> lines)
        {
            foreach (var productGroup in lines.GroupBy(x => x.Product.ProductId))
            {
                var transaction = new InventoryTransactions
                {
                    ProductId = productGroup.Key,
                    Type = InventoryTransactionType.Export.ToRecordValue(),
                    UserId = userId,
                    SupplierId = null,
                    Note = $"Xuất kho cho đơn hàng #{orderId}",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    InventoryTransactionsDetail = productGroup.Select(line => new InventoryTransactionsDetail
                    {
                        VarientId = line.Variant.VarientId,
                        Quantity = line.Quantity,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    }).ToList()
                };

                _context.InventoryTransactions.Add(transaction);
            }
        }

        private async Task RemovePurchasedCartItemsAsync(int userId, IEnumerable<CheckoutLine> lines, CancellationToken cancellationToken)
        {
            var variantIds = lines.Select(x => x.Variant.VarientId).Distinct().ToList();
            var cartItems = await _context.CartItems
                .Include(x => x.Cart)
                .Where(x => x.Cart.UserId == userId && variantIds.Contains(x.VarientProductId))
                .ToListAsync(cancellationToken);

            if (cartItems.Count > 0)
            {
                _context.CartItems.RemoveRange(cartItems);
            }
        }

        private async Task<Voucher?> GetValidatedVoucherAsync(string? voucherCode, decimal? totalAmount, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(voucherCode))
            {
                return null;
            }

            var normalizedCode = voucherCode.Trim().ToLowerInvariant();
            var voucher = await _context.Vouchers.FirstOrDefaultAsync(x => x.Code.ToLower().Trim() == normalizedCode, cancellationToken);
            if (voucher == null)
            {
                throw new InvalidOperationException("Voucher không đúng.");
            }

            var validationMessage = ValidateVoucher(voucher);
            if (!string.IsNullOrEmpty(validationMessage))
            {
                throw new InvalidOperationException(validationMessage);
            }

            if (totalAmount.HasValue)
            {
                var discount = ParseCurrency(voucher.Promotion, totalAmount.Value);
                if (discount < 0)
                {
                    throw new InvalidOperationException("Giá trị giảm giá không hợp lệ.");
                }
            }

            return voucher;
        }

        private static string? ValidateVoucher(Voucher voucher)
        {
            var today = DateTime.Now;
            if (voucher.StartedAt.HasValue && voucher.StartedAt.Value > today)
            {
                return "Voucher chưa đến thời hạn sử dụng.";
            }

            if (voucher.ExpiredAt.HasValue && voucher.ExpiredAt.Value < today)
            {
                return "Voucher đã hết hạn.";
            }

            if (voucher.Quantity <= 0)
            {
                return "Voucher đã hết.";
            }

            return null;
        }

        private void ConsumeVoucher(Voucher? voucher)
        {
            if (voucher == null)
            {
                return;
            }

            if (voucher.Quantity <= 0)
            {
                throw new InvalidOperationException("Voucher đã hết.");
            }

            voucher.Quantity -= 1;
            voucher.UpdatedAt = DateTime.Now;
            _context.Vouchers.Update(voucher);
        }

        private async Task<Address> AddShipAddressAsync(PaymentDTo paymentDTo, int userId, CancellationToken cancellationToken)
        {
            var address = new Address
            {
                UserId = userId,
                AddressLine = paymentDTo.Address?.AddressLine,
                Ward = paymentDTo.Address?.Ward,
                District = paymentDTo.Address?.District,
                Province = paymentDTo.Address?.Province
            };

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync(cancellationToken);
            return address;
        }

        private async Task<string?> BuildFormattedAddressAsync(Address? address, CancellationToken cancellationToken)
        {
            if (address == null ||
                string.IsNullOrEmpty(address.AddressLine) ||
                string.IsNullOrEmpty(address.Province) ||
                string.IsNullOrEmpty(address.Ward) ||
                string.IsNullOrEmpty(address.District))
            {
                return null;
            }

            var jsonString = await File.ReadAllTextAsync("wwwroot/Province_VN.json", cancellationToken);
            var provinces = JsonConvert.DeserializeObject<List<Province>>(jsonString);
            var province = provinces?.FirstOrDefault(p => p.Code == int.Parse(address.Province));
            var district = province?.Districts?.FirstOrDefault(d => d.Code == int.Parse(address.District));
            var ward = district?.Wards?.FirstOrDefault(w => w.Code == int.Parse(address.Ward));

            return $"{address.AddressLine},{ward?.Name}, {district?.Name}, {province?.Name}";
        }

        private async Task<List<CheckoutDTo>> GetProductDetailsAsync(List<CartDTo> cartItems, CancellationToken cancellationToken)
        {
            var productIds = cartItems.Select(x => x.ProductId).Distinct().ToList();
            var variantIds = cartItems.Select(x => x.VarientProductId).Distinct().ToList();

            var products = await _context.Products
                .Where(x => productIds.Contains(x.ProductId))
                .ToDictionaryAsync(x => x.ProductId, cancellationToken);

            var variants = await _context.VarientProducts
                .Where(x => variantIds.Contains(x.VarientId))
                .ToDictionaryAsync(x => x.VarientId, cancellationToken);

            var productDetails = new List<CheckoutDTo>();

            foreach (var item in cartItems)
            {
                if (!products.TryGetValue(item.ProductId, out var product))
                {
                    continue;
                }

                variants.TryGetValue(item.VarientProductId, out var variantProduct);

                productDetails.Add(new CheckoutDTo
                {
                    ProductId = product.ProductId,
                    ProductName = product.Name,
                    ImageUrl = variantProduct?.ImageUrl,
                    Quantity = item.Quantity,
                    VarientId = item.VarientProductId,
                    SellPrice = variantProduct?.Price ?? 0,
                    OriginPrice = product.OriginalPrice,
                    Attributes = variantProduct?.Attributes
                });
            }

            return productDetails;
        }

        private async Task<decimal> GetTotalPriceAsync(PaymentDTo model, CancellationToken cancellationToken)
        {
            var checkoutContext = await BuildCheckoutContextAsync(model, cancellationToken);
            var originAmount = checkoutContext.Lines.Sum(x => x.UnitPrice * x.Quantity);
            var voucher = await GetValidatedVoucherAsync(model.VoucherCode, originAmount, cancellationToken);
            var discountAmount = voucher == null ? 0 : ParseCurrency(voucher.Promotion, originAmount);
            return Math.Max(0, originAmount - discountAmount + GetShippingAmount(checkoutContext.Lines.Count));
        }

        private static int GetShippingAmount(int productCount)
        {
            return productCount > 0 ? DefaultShippingAmount : 0;
        }

        private decimal ParseCurrency(string? input, decimal totalPrice)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return 0;
            }

            if (input.EndsWith("%", StringComparison.Ordinal))
            {
                var percentage = decimal.Parse(input.TrimEnd('%')) / 100;
                return totalPrice * percentage;
            }

            input = input.Replace("₫", string.Empty).Replace(",", string.Empty).Trim();
            return decimal.Parse(input);
        }

        private static string ResolveStockStatus(int stock, string? currentStatus)
        {
            if (stock <= 0)
            {
                return "outstock";
            }

            if (string.Equals(currentStatus, "discontinued", StringComparison.OrdinalIgnoreCase))
            {
                return "discontinued";
            }

            if (string.Equals(currentStatus, "preorder", StringComparison.OrdinalIgnoreCase))
            {
                return "preorder";
            }

            return "available";
        }

        private async Task NotifyOrderCreatedAsync(User user, int orderId)
        {
            try
            {
                await _notificationService.NotifyAsync(
                    NotificationTarget.Admins,
                    "Xác nhận đơn hàng",
                    $"Có đơn hàng mới từ người dùng {user.Email}!",
                    "new order",
                    $"/admin/orders/view/{orderId}");
            }
            catch
            {
            }

            try
            {
                await _notificationService.NotifyAsync(
                    NotificationTarget.SpecificUsers,
                    "Xác nhận đơn hàng",
                    "Đơn hàng của bạn đang được xử lý.",
                    "success",
                    $"/user/MyOrders/OrderDetail/{orderId}",
                    new List<int> { user.UserId });
            }
            catch
            {
            }
        }

        private async Task NotifyGatewayPaymentSuccessAsync(User user, int orderId)
        {
            try
            {
                await _notificationService.NotifyAsync(
                    NotificationTarget.Admins,
                    "Thanh toán thành công",
                    $"Đơn hàng {orderId} đã được thanh toán thành công!",
                    "payment received",
                    "/admin/transactions");
            }
            catch
            {
            }

            try
            {
                await _notificationService.NotifyAsync(
                    NotificationTarget.SpecificUsers,
                    "Thanh toán thành công",
                    $"Đơn hàng {orderId} đã được thanh toán thành công!",
                    "success",
                    $"/user/MyOrders/OrderDetail/{orderId}",
                    new List<int> { user.UserId });
            }
            catch
            {
            }
        }

        private async Task TrackPurchaseEventsAsync(
            int userId,
            int orderId,
            PaymentMethodType paymentMethodType,
            IEnumerable<CheckoutLine> lines,
            CancellationToken cancellationToken)
        {
            foreach (var line in lines)
            {
                var metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    orderId,
                    paymentMethod = paymentMethodType.ToCode(),
                    quantity = line.Quantity,
                    unitPrice = line.UnitPrice
                });

                await _userProductEventTrackingService.TrackAsync(new UserProductEventWriteRequest
                {
                    UserId = userId,
                    ProductId = line.Product.ProductId,
                    ProductSysId = line.Product.ProductSysId,
                    EventType = UserProductEventType.Purchase,
                    Source = "checkout_order",
                    MetadataJson = metadata
                }, cancellationToken);
            }
        }

        private sealed class CheckoutContext
        {
            public List<CheckoutLine> Lines { get; set; } = new();
        }

        private sealed class CheckoutLine
        {
            public Product Product { get; set; } = null!;
            public Models.VarientProduct Variant { get; set; } = null!;
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
        }

        private sealed class CheckoutRequestLine
        {
            public int VariantId { get; set; }
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }
    }
}
