$(document).ready(function () {
    const productSelections = [];
    const customerPickerState = {
        page: 1,
        pageSize: 10,
        keyword: ""
    };
    const voucherPickerState = {
        keyword: ""
    };
    let provinceDataPromise = null;

    function renderPosProductCard(product) {
        const image = product.image?.startsWith("http")
            ? product.image
            : `/Upload/Products/${product.image || "no-image.png"}`;
        const price = product.sellPrice || "Giá chưa cập nhật";
        const sku = product.sku || "SKU cập nhật sau";

        return `
            <div class="col-6 col-lg-4">
                <button class="pos-product-card getProductInfo" type="button" data-id="${product.productId}">
                    <span class="pos-product-card__image-shell">
                        <img src="${image}" class="pos-product-card__image" alt="${product.name}">
                    </span>
                    <span class="pos-product-card__body">
                        <span class="pos-product-card__title">${product.name}</span>
                        <span class="pos-product-card__sku">${sku}</span>
                        <span class="pos-product-card__price">${price}</span>
                    </span>
                </button>
            </div>
        `;
    }

    function escapeHtml(value) {
        return String(value ?? "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }

    function showLoading(show) {
        $(".loading-container").toggle(!!show);
    }

    function parseMoney(text) {
        const normalized = String(text ?? "").replace(/[.,đ\s]/g, "");
        const parsed = parseInt(normalized, 10);
        return Number.isNaN(parsed) ? 0 : parsed;
    }

    function formatMoney(value) {
        return `${Number(value || 0).toLocaleString("vi-VN")}đ`;
    }

    function updateCartEmptyState() {
        const $tbody = $("#shoppingCartBody");
        const hasProducts = $tbody.find("tr").not(".cart-empty-state").length > 0;

        if (hasProducts) {
            $tbody.find(".cart-empty-state").remove();
            return;
        }

        if ($tbody.find(".cart-empty-state").length === 0) {
            $tbody.html(`
                <tr class="cart-empty-state">
                    <td colspan="5" class="text-center text-muted py-4">
                        <i class="fa fa-shopping-basket mb-2 d-block" style="font-size:2rem;opacity:.55;"></i>
                        Chưa có sản phẩm trong giỏ hàng
                    </td>
                </tr>
            `);
        }
    }

    function showCustomerInfoEmpty() {
        $("#customerInfoEmpty").attr("style", "");
        $("#customerInfoGrid").addClass("d-none");
        $("#customerInfoEmpty").removeClass("d-none");
        $("#nameCustomer").text("-");
        $("#phoneCustomer").text("-");
        $("#addressCustomer").text("-");
    }

    function showCustomerInfo(name, phone, address) {
        $("#customerInfoGrid").attr("style", "");
        $("#customerInfoEmpty").addClass("d-none");
        $("#customerInfoGrid").removeClass("d-none");
        $("#nameCustomer").text(name || "-");
        $("#phoneCustomer").text(phone || "-");
        $("#addressCustomer").text(address || "-");
    }

    function resetVoucherDisplay() {
        $("#Discount").text("0đ");
        $("#resultVoucher").hide().removeClass("text-success").addClass("text-danger").text("");
        $("#selectedVoucherSummary").hide().empty();
        updateTotalPayment();
    }

    function applyVoucherDisplay(voucher) {
        if (!voucher) {
            resetVoucherDisplay();
            return;
        }

        $("#resultVoucher")
            .text("Voucher đã được áp dụng thành công")
            .removeClass("text-danger")
            .addClass("text-success")
            .show();

        if (voucher.promotion && voucher.promotion.endsWith("%")) {
            $("#Discount").text(voucher.promotion);
        } else {
            const promotionAmount = parseFloat(String(voucher.promotion || "0").replace(/,/g, ""));
            $("#Discount").text(formatMoney(promotionAmount));
        }

        const expiryText = voucher.expiredAt
            ? new Date(voucher.expiredAt).toLocaleDateString("vi-VN")
            : "Không giới hạn";

        $("#selectedVoucherSummary")
            .html(`
                <div class="alert alert-light border mb-0">
                    <div class="fw-semibold">${escapeHtml(voucher.code || "")} - ${escapeHtml(voucher.name || "")}</div>
                    <div class="small text-muted">Giảm: ${escapeHtml(voucher.promotion || "")} | Hết hạn: ${escapeHtml(expiryText)}</div>
                </div>
            `)
            .show();

        updateTotalPayment();
    }

    function fetchProvinceData() {
        if (!provinceDataPromise) {
            provinceDataPromise = fetch("/Province_VN.json").then(function (response) {
                if (!response.ok) {
                    throw new Error("Không thể tải dữ liệu địa giới");
                }
                return response.json();
            });
        }

        return provinceDataPromise;
    }

    async function resolveAddressRecord(address) {
        if (!address) {
            return "";
        }

        const allData = await fetchProvinceData();
        const province = allData.find(function (item) {
            return String(item.code) === String(address.province);
        });

        let district = null;
        let ward = null;

        for (const provinceItem of allData) {
            district = provinceItem.districts.find(function (item) {
                return String(item.code) === String(address.district);
            });

            if (district) {
                ward = district.wards.find(function (item) {
                    return String(item.code) === String(address.ward);
                });
                break;
            }
        }

        return [address.addressLine, ward?.name, district?.name, province?.name]
            .filter(function (part) {
                return !!part;
            })
            .join(", ");
    }

    async function resolveAddress(addresses) {
        if (!Array.isArray(addresses) || addresses.length === 0) {
            return "";
        }

        return resolveAddressRecord(addresses[0]);
    }

    function updateTotalPrice() {
        let total = 0;

        $("#shopping-cart .price-cell").each(function () {
            const unitPrice = Number($(this).data("price") || 0);
            const quantity = Number($(this).closest("tr").find(".pos-cart-quantity").text() || 0);
            total += unitPrice * quantity;
        });

        $("#TotalPrice").text(formatMoney(total));
        updateTotalPayment();
        updateCartEmptyState();
    }

    function updateTotalPayment() {
        const totalPrice = parseMoney($("#TotalPrice").text());
        const deductPrice = parseMoney($("#Deduct").text());
        const discountText = $("#Discount").text().trim();
        let discountPrice = 0;

        if (discountText.includes("%")) {
            const percent = parseFloat(discountText.replace("%", ""));
            if (!Number.isNaN(percent)) {
                discountPrice = totalPrice * percent / 100;
            }
        } else {
            discountPrice = parseMoney(discountText);
        }

        const total = Math.max(totalPrice - deductPrice - discountPrice, 0);
        $("#TotalPayment").text(formatMoney(total));
    }

    function renderCustomerRows(customers) {
        if (!customers.length) {
            return `
                <tr>
                    <td colspan="6" class="text-center text-muted py-4">Không tìm thấy khách hàng phù hợp.</td>
                </tr>
            `;
        }

        return customers.map(function (customer) {
            const fullName = escapeHtml(customer.fullName || "-");
            const phoneNumber = escapeHtml(customer.phoneNumber || "-");
            const email = escapeHtml(customer.email || "-");
            const address = escapeHtml(customer.resolvedAddress || "-");

            return `
                <tr>
                    <td>#${customer.userId}</td>
                    <td>
                        <div class="fw-semibold text-dark">${fullName}</div>
                    </td>
                    <td>${phoneNumber}</td>
                    <td>${email}</td>
                    <td>${address}</td>
                    <td class="text-center">
                        <button type="button"
                                class="btn btn-outline-primary btn-sm select-customer-btn px-2 py-1"
                                data-id="${customer.userId}"
                                data-name="${escapeHtml(customer.fullName || "")}"
                                title="Chọn khách hàng"
                                aria-label="Chọn khách hàng">
                            <i class="fa fa-arrow-right"></i>
                        </button>
                    </td>
                </tr>
            `;
        }).join("");
    }

    function renderCustomerPagination(page, totalPages) {
        if (totalPages <= 1) {
            return "";
        }

        let buttons = `
            <button type="button" class="btn btn-sm btn-light border customer-page-btn" data-page="${page - 1}" ${page <= 1 ? "disabled" : ""}>
                Trước
            </button>
        `;

        for (let current = 1; current <= totalPages; current += 1) {
            buttons += `
                <button type="button" class="btn btn-sm ${current === page ? "btn-primary" : "btn-light border"} customer-page-btn" data-page="${current}">
                    ${current}
                </button>
            `;
        }

        buttons += `
            <button type="button" class="btn btn-sm btn-light border customer-page-btn" data-page="${page + 1}" ${page >= totalPages ? "disabled" : ""}>
                Sau
            </button>
        `;

        return buttons;
    }

    function loadCustomers() {
        showLoading(true);

        $.ajax({
            url: "/Admin/POS/GetCustomers",
            data: {
                keyword: customerPickerState.keyword,
                page: customerPickerState.page,
                pageSize: customerPickerState.pageSize
            },
            success: async function (res) {
                if (!res.success) {
                    $("#customerPickerTableBody").html(`
                        <tr>
                            <td colspan="6" class="text-center text-danger py-4">Không thể tải danh sách khách hàng.</td>
                        </tr>
                    `);
                    return;
                }

                const customers = await Promise.all((res.customers || []).map(async function (customer) {
                    const resolvedAddress = await resolveAddressRecord({
                        addressLine: customer.addressLine,
                        ward: customer.ward,
                        district: customer.district,
                        province: customer.province
                    });

                    return {
                        ...customer,
                        resolvedAddress: resolvedAddress || customer.address || "-"
                    };
                }));

                $("#customerPickerTableBody").html(renderCustomerRows(customers));
                $("#customerPickerMeta").text(`Tổng ${res.totalItems} khách hàng, trang ${res.page}/${res.totalPages}`);
                $("#customerPickerPagination").html(renderCustomerPagination(res.page, res.totalPages));
            },
            error: function () {
                $("#customerPickerTableBody").html(`
                    <tr>
                        <td colspan="6" class="text-center text-danger py-4">Đã xảy ra lỗi khi tải danh sách khách hàng.</td>
                    </tr>
                `);
            },
            complete: function () {
                showLoading(false);
            }
        });
    }

    function renderVoucherCards(vouchers) {
        if (!vouchers.length) {
            return `
                <div class="col-12">
                    <div class="d-flex flex-column align-items-center justify-content-center text-center text-muted py-5">
                        <i class="fa fa-ticket-alt mb-3" style="font-size:2rem;opacity:.55;"></i>
                        <div class="fw-semibold">Không có voucher hợp lệ</div>
                    </div>
                </div>
            `;
        }

        return vouchers.map(function (voucher) {
            const expiryText = voucher.expiredAt
                ? new Date(voucher.expiredAt).toLocaleDateString("vi-VN")
                : "Không giới hạn";

            return `
                <div class="col-md-6">
                    <button type="button"
                            class="btn btn-light border w-100 text-start h-100 p-3 select-voucher-btn"
                            data-code="${escapeHtml(voucher.code || "")}">
                        <div class="d-flex justify-content-between align-items-start gap-3">
                            <div>
                                <div class="fw-semibold text-dark">${escapeHtml(voucher.code || "")}</div>
                                <div class="small text-muted">${escapeHtml(voucher.name || "Không tên")}</div>
                            </div>
                            <span class="badge bg-success">${escapeHtml(voucher.promotion || "")}</span>
                        </div>
                        <div class="small text-muted mt-3">Hạn dùng: ${escapeHtml(expiryText)}</div>
                        <div class="small text-muted">Số lượng còn: ${escapeHtml(voucher.quantity ?? 0)}</div>
                    </button>
                </div>
            `;
        }).join("");
    }

    function loadAvailableVouchers() {
        showLoading(true);

        $.ajax({
            url: "/Admin/POS/GetAvailableVouchers",
            data: {
                keyword: voucherPickerState.keyword
            },
            success: function (res) {
                if (!res.success) {
                    $("#voucherPickerList").html(`
                        <div class="col-12">
                            <div class="text-center text-danger py-4">Không thể tải voucher.</div>
                        </div>
                    `);
                    return;
                }

                $("#voucherPickerList").html(renderVoucherCards(res.vouchers || []));
            },
            error: function () {
                $("#voucherPickerList").html(`
                    <div class="col-12">
                        <div class="text-center text-danger py-4">Đã xảy ra lỗi khi tải voucher.</div>
                    </div>
                `);
            },
            complete: function () {
                showLoading(false);
            }
        });
    }

    function loadCustomerDetail(id, fullName) {
        showLoading(true);

        $.ajax({
            url: "/Admin/POS/GetUser",
            data: { id: id },
            success: async function (res) {
                if (res.success === false) {
                    alert(res.message || "Không tìm thấy khách hàng.");
                    return;
                }

                const resolvedAddress = await resolveAddress(res.addresses || []);
                const resolvedName = fullName || `${res.lastName || ""} ${res.firstName || ""}`.trim() || "-";
                const resolvedPhone = res.phoneNumber || "-";
                const resolvedAddressText = resolvedAddress || "-";

                $("#selectedCustomerId").val(res.userId);
                $("#selectedCustomerLabel").text(resolvedName);
                showCustomerInfo(resolvedName, resolvedPhone, resolvedAddressText);
                $("#CustomerPickerModal").modal("hide");
            },
            error: function (xhr) {
                alert("Đã có lỗi xảy ra: " + xhr.responseText);
            },
            complete: function () {
                showLoading(false);
            }
        });
    }

    function validateAndApplyVoucher(code) {
        if (!code || code.trim().length <= 3) {
            $("#resultVoucher")
                .text("Mã voucher không hợp lệ.")
                .removeClass("text-success")
                .addClass("text-danger")
                .show();
            return;
        }

        showLoading(true);

        $.ajax({
            url: "/Admin/POS/CheckVoucher",
            data: { code: code.trim() },
            success: function (res) {
                if (res.success) {
                    applyVoucherDisplay(res.voucher);
                } else {
                    $("#resultVoucher")
                        .text(res.message)
                        .removeClass("text-success")
                        .addClass("text-danger")
                        .show();
                    $("#selectedVoucherSummary").hide().empty();
                    $("#Discount").text("0đ");
                    updateTotalPayment();
                }
            },
            error: function (_, message) {
                $("#resultVoucher")
                    .text("Có lỗi xảy ra: " + message)
                    .removeClass("text-success")
                    .addClass("text-danger")
                    .show();
            },
            complete: function () {
                showLoading(false);
            }
        });
    }

    $(document).on("click", ".getProductInfo", function () {
        const id = $(this).data("id");
        showLoading(true);

        $.ajax({
            url: "/Admin/POS/GetProduct",
            data: { id: id },
            success: function (res) {
                if (!res.success) {
                    alert(res.message);
                    return;
                }

                $("#nameProduct").text(res.product.name);
                $("#priceProduct").text(res.product.sellPrice);
                $("#originalPriceProduct").text(res.product.originalPrice);
                $("#categoryName").text(res.product.category.name);
                $("#brandName").text(res.product.brand.name);
                $("#sku").text(res.product.sku);

                if (res.product.image.startsWith("http")) {
                    $("#imgProduct").attr("src", res.product.image);
                } else {
                    $("#imgProduct").attr("src", "/Upload/Products/" + res.product.image);
                }

                $("#IdProduct").val(res.product.productId);

                if (res.product.stock > 0) {
                    $(".stockValue")
                        .html('<i class="fas fa-check-circle"></i> Còn hàng')
                        .removeClass("bg-danger")
                        .addClass("bg-success");
                } else {
                    $(".stockValue")
                        .html('<i class="fas fa-times-circle"></i> Hết hàng')
                        .removeClass("bg-success")
                        .addClass("bg-danger");
                }

                const variantContainer = $("#variantCardsContainer");
                variantContainer.empty();

                if (res.product.varientProducts && res.product.varientProducts.length > 0) {
                    let allAvailable = true;

                    res.product.varientProducts
                        .sort(function (a, b) {
                            if (a.price && b.price) {
                                return a.price - b.price;
                            }
                            return 0;
                        })
                        .forEach(function (variant, index) {
                            const variantLabel = Array.isArray(variant.values) && variant.values.length > 0
                                ? variant.values.join(" / ")
                                : (variant.sku || "Mặc định");

                            variantContainer.append(`
                                <div class="variant-card ${variant.stock > 0 && variant.price != null ? "" : "out-of-stock"}" data-id="${variant.varientId}">
                                    <div class="variant-card__label">${escapeHtml(variantLabel)}</div>
                                    <div class="variant-card__meta">${escapeHtml(variant.sku || "")}</div>
                                    <div class="variant-card__price">${variant.price != null ? Number(variant.price).toLocaleString("vi-VN") + " đ" : "Giá chưa có"}</div>
                                    ${variant.stock > 0
                                        ? '<div class="variant-card__stock variant-card__stock--available">Còn hàng</div>'
                                        : '<div class="variant-card__stock variant-card__stock--empty">Hết hàng</div>'}
                                </div>
                            `);

                            if (index === 0) {
                                variantContainer.find(".variant-card").first().addClass("selected");
                            }

                            if (variant.stock <= 0 || variant.price == null) {
                                allAvailable = false;
                            }
                        });

                    $("#AddToCart").prop("disabled", !allAvailable);
                } else {
                    variantContainer.append("<p>Không có biến thể nào cho sản phẩm này.</p>");
                }

                $("#productDetail").modal("show");
            },
            error: function (xhr) {
                alert("Đã có lỗi: " + xhr.responseText);
            },
            complete: function () {
                showLoading(false);
            }
        });
    });

    $(document).on("click", ".variant-card", function () {
        if ($(this).hasClass("out-of-stock")) {
            return;
        }

        $(".variant-card").removeClass("selected");
        $(this).addClass("selected");
    });

    let searchTimeout = null;
    $(".searchProducts").on("input", function () {
        clearTimeout(searchTimeout);
        const name = $(this).val().trim();
        const cateId = $('select[name="CateId"]').val();

        searchTimeout = setTimeout(function () {
            $.ajax({
                url: "/Admin/POS/GetProducts",
                data: { name: name, cateId: cateId },
                success: function (res) {
                    if (!res.success) {
                        $("#CardProducts .row").html('<div class="col-12"><p class="text-center">Đã xảy ra lỗi khi tìm kiếm sản phẩm.</p></div>');
                        return;
                    }

                    let productsHtml = "";
                    if (res.products.length > 0) {
                        $.each(res.products, function (_, product) {
                            productsHtml += renderPosProductCard(product);
                        });
                    } else {
                        productsHtml = '<div class="col-12"><p class="text-center">Không tìm thấy sản phẩm nào.</p></div>';
                    }

                    $("#CardProducts .row").html(productsHtml);
                },
                error: function () {
                    $("#CardProducts .row").html('<div class="col-12"><p class="text-center">Đã xảy ra lỗi kết nối.</p></div>');
                }
            });
        }, 300);
    });

    $('select[name="CateId"]').change(function () {
        const id = $(this).val();
        const name = $('input[name="searchProducts"]').val();
        showLoading(true);

        $.ajax({
            url: "/Admin/POS/GetProducts",
            data: { name: name, cateId: id },
            success: function (res) {
                if (!res.success) {
                    alert(res.message);
                    return;
                }

                let productsHtml = "";
                $.each(res.products, function (_, product) {
                    productsHtml += renderPosProductCard(product);
                });
                $("#CardProducts .row").html(productsHtml);
            },
            error: function (xhr) {
                alert("Đã có lỗi xảy ra: " + xhr.responseText);
            },
            complete: function () {
                showLoading(false);
            }
        });
    });

    $("#Checkvoucher").click(function () {
        validateAndApplyVoucher($('input[name="Voucher"]').val());
    });

    $(document).on("click", ".select-customer-btn", function () {
        const id = $(this).data("id");
        const fullName = $(this).closest("tr").find(".fw-semibold").text().trim();
        loadCustomerDetail(id, fullName);
    });

    $(document).on("click", ".customer-page-btn", function () {
        const page = Number($(this).data("page") || 1);
        if (page < 1) {
            return;
        }
        customerPickerState.page = page;
        loadCustomers();
    });

    $("#searchCustomerBtn").click(function () {
        customerPickerState.keyword = $("#customerKeyword").val().trim();
        customerPickerState.page = 1;
        loadCustomers();
    });

    $("#resetCustomerBtn").click(function () {
        $("#customerKeyword").val("");
        customerPickerState.keyword = "";
        customerPickerState.page = 1;
        loadCustomers();
    });

    $("#CustomerPickerModal").on("shown.bs.modal", function () {
        loadCustomers();
    });

    $("#searchVoucherBtn").click(function () {
        voucherPickerState.keyword = $("#voucherKeyword").val().trim();
        loadAvailableVouchers();
    });

    $("#resetVoucherBtn").click(function () {
        $("#voucherKeyword").val("");
        voucherPickerState.keyword = "";
        loadAvailableVouchers();
    });

    $("#VoucherPickerModal").on("shown.bs.modal", function () {
        loadAvailableVouchers();
    });

    $(document).on("click", ".select-voucher-btn", function () {
        const code = $(this).data("code");
        $('input[name="Voucher"]').val(code);
        $("#VoucherPickerModal").modal("hide");
        validateAndApplyVoucher(code);
    });

    $("#AddToCart").click(function () {
        showLoading(true);
        const selectedVariant = $(".variant-card.selected");
        const variantId = selectedVariant.data("id");
        const productId = $("#IdProduct").val();
        const quantity = $('input[name="quantityValue"]').val();

        $("#productDetail").modal("hide");

        if (variantId == null) {
            showLoading(false);
            return;
        }

        const existed = productSelections.find(function (item) {
            return item.VarientProductId === variantId;
        });

        if (existed) {
            showLoading(false);
            return;
        }

        $.ajax({
            url: "/Admin/POS/AddToCart",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify({
                VarientProductId: variantId,
                ProductId: productId,
                Quantity: quantity
            }),
            success: function (response) {
                if (!response.success) {
                    alert(response.message);
                    return;
                }

                $("#shoppingCartBody").find(".cart-empty-state").remove();
                productSelections.push({
                    VarientProductId: variantId,
                    Quantity: parseInt(quantity, 10)
                });

                $("#shoppingCartBody").append(response.result);
                updateTotalPrice();
            },
            error: function () {
                alert("Đã xảy ra lỗi khi thêm sản phẩm vào giỏ hàng.");
            },
            complete: function () {
                showLoading(false);
            }
        });
    });

    $("#shopping-cart").on("click", ".btn-delete", function () {
        const variantId = $(this).closest("tr").data("varient-id");
        const index = productSelections.findIndex(function (item) {
            return item.VarientProductId === variantId;
        });

        if (index > -1) {
            productSelections.splice(index, 1);
        }

        $(this).closest("tr").remove();
        updateTotalPrice();
    });

    $(".btn-deduct").on("click", function () {
        const deductValue = $('input[name="DeductValue"]').val();
        $("#Deduct").text(formatMoney(deductValue));
        $("#ModalDeduct").modal("hide");
        updateTotalPayment();
    });

    $(".btn-discount").on("click", function () {
        const discountValue = $('input[name="DiscountValue"]').val();
        $("#Discount").text(formatMoney(discountValue));
        $("#ModalDiscount").modal("hide");
        updateTotalPayment();
    });

    $("#ModalDeduct").on("hidden.bs.modal", function () {
        $('input[name="DeductValue"]').val(0);
        showLoading(false);
    });

    $("#ModalDiscount").on("hidden.bs.modal", function () {
        $('input[name="DiscountValue"]').val(0);
        showLoading(false);
    });

    $("#productDetail").on("hidden.bs.modal", function () {
        $("#nameProduct").text("");
        $("#priceProduct").text("");
        $("#originalPriceProduct").text("");
        $("#categoryName").text("");
        $("#brandName").text("");
        $("#sku").text("");
        $('input[name="quantityValue"]').val(1);
        $("#imgProduct").attr("src", "/Upload/Products/no-image.png");
        $("#IdProduct").val("");
        showLoading(false);
    });

    $(".btn-Order").click(function () {
        const userId = $("#selectedCustomerId").val();
        const totalAmount = parseMoney($("#TotalPayment").text());
        const originAmount = parseMoney($("#TotalPrice").text());
        const voucher = $('input[name="Voucher"]').val();
        const deductPrice = $("#Deduct").text().replace(/[.,đ\s]/g, "");
        const discountPrice = $("#Discount").text().replace(/[.,đ\s]/g, "");
        const paymentMethod = $('input[name="type"]:checked').val();

        if (!userId) {
            Swal.fire({
                icon: "warning",
                title: "Chưa chọn khách hàng",
                text: "Bạn cần chọn khách hàng trước khi đặt đơn."
            });
            return;
        }

        if (!productSelections.length) {
            Swal.fire({
                icon: "warning",
                title: "Giỏ hàng đang trống",
                text: "Bạn cần thêm ít nhất một sản phẩm."
            });
            return;
        }

        Swal.fire({
            title: "Bạn có chắc chắn muốn đặt đơn hàng này?",
            text: "Hãy kiểm tra lại thông tin trước khi xác nhận.",
            icon: "warning",
            showCancelButton: true,
            confirmButtonText: "Đặt hàng",
            cancelButtonText: "Hủy",
            reverseButtons: true
        }).then(function (result) {
            if (!result.isConfirmed) {
                return;
            }

            showLoading(true);

            $.ajax({
                method: "POST",
                url: "/Admin/POS/Order",
                contentType: "application/json",
                data: JSON.stringify({
                    UserId: userId,
                    TotalPrice: totalAmount,
                    OriginTotalPrice: originAmount,
                    DeductPrice: deductPrice,
                    DiscountPrice: discountPrice,
                    ListVarientProduct: productSelections,
                    PaymentMethod: paymentMethod,
                    Voucher: voucher
                }),
                success: function (res) {
                    if (res.success) {
                        Swal.fire({
                            icon: "success",
                            title: "Đặt hàng thành công!",
                            text: "Đơn hàng của bạn đã được tạo thành công. Chuyển đến hóa đơn...",
                            confirmButtonText: "Xem hóa đơn"
                        }).then(function () {
                            window.location.href = "/Admin/POS/Invoice/" + res.id;
                        });
                    } else {
                        Swal.fire({
                            icon: "error",
                            title: "Đặt hàng không thành công",
                            text: res.message || "Vui lòng thử lại sau."
                        });
                    }
                },
                error: function (res) {
                    Swal.fire({
                        icon: "error",
                        title: "Đã có lỗi xảy ra",
                        text: res.responseJSON ? res.responseJSON.message : "Vui lòng thử lại."
                    });
                },
                complete: function () {
                    showLoading(false);
                }
            });
        });
    });

    showCustomerInfoEmpty();
    updateCartEmptyState();
});
