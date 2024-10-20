
$(document).ready(function () {
    // Sử dụng delegated events để đảm bảo sự kiện click hoạt động trên các phần tử mới
    $(document).ready(function () {
        // Sử dụng delegated events để đảm bảo sự kiện click hoạt động trên các phần tử mới
        $(document).ready(function () {
            // Sử dụng delegated events để đảm bảo sự kiện click hoạt động trên các phần tử mới
            $(document).on('click', '.getProductInfo', function () {
                var id = $(this).data('id');
                let url = '/Admin/POS/GetProduct';
                $('.loading-container').show();
                $.ajax({
                    url: url,
                    data: { id: id },
                    success: function (res) {
                        if (res.success) {
                            // Hiển thị thông tin sản phẩm
                            $('#nameProduct').text(res.product.name);
                            $('#priceProduct').text(res.product.sellPrice);
                            $('#categoryName').text(res.product.category.name);
                            $('#brandName').text(res.product.brand.name);
                            $('#sku').text(res.product.sku)
                            $('#imgProduct').attr('src', '/Upload/Products/' + res.product.image);
                            $('#IdProduct').val(res.product.productId);
                            //Hiển thị số lượng tồn
                            if (res.product.stock > 0) {
                                $('.stockValue')
                                    .html('<i class="fas fa-check-circle"></i> Còn Hàng')
                                    .removeClass('bg-danger') // Loại bỏ class bg-danger nếu có
                                    .addClass('bg-success');  // Thêm class bg-success
                            } else {
                                $('.stockValue')
                                    .html('<i class="fas fa-times-circle"></i> Hết Hàng')
                                    .removeClass('bg-success') // Loại bỏ class bg-success nếu có
                                    .addClass('bg-danger');     // Thêm class bg-danger
                            }

                            // Hiển thị các thuộc tính biến thể
                            var variantContainer = $('#variantCardsContainer');
                            variantContainer.empty(); // Xóa nội dung cũ
                            if (res.product.varientProducts && res.product.varientProducts.length > 0) {
                                let allAvailable = true; // Biến để kiểm tra xem có tất cả sản phẩm có hàng và có giá

                                res.product.varientProducts.forEach(function (variant) {
                                var variantCard = `
                                   <div class="variant-card ${variant.stock > 0 && variant.price != null ? '' : 'out-of-stock'}" data-id="${variant.varientId}">
                                        <h6>${variant.sku}</h6>
                                        <p>${variant.price != null ? variant.price.toLocaleString('vi-VN') + ' đ' : 'Giá chưa có'}</p>
                                        ${variant.stock > 0 ? `
                                            <p class="text-success">Còn hàng</p>
                                        ` : '<span class="text-danger">Hết hàng</span>'}
                                    </div>

                                `;

                                    variantContainer.append(variantCard);

                                    // Kiểm tra điều kiện hàng hóa và giá
                                    if (res.product.stock <= 0) {
                                        allAvailable = false; // Nếu có bất kỳ sản phẩm nào không có hàng hoặc không có giá
                                    }
                                });

                                // Vô hiệu hóa nút AddToCart nếu không có hàng hoặc không có giá
                                $('#AddToCart').prop('disabled', !allAvailable);

                            } else {
                                variantContainer.append('<p>Không có biến thể nào cho sản phẩm này.</p>');
                            }
                            $('#productDetail').modal('show');
                        } else {
                            alert(res.message);
                        }
                        $('.loading-container').hide();
                    },
                    error: function (xhr, status, error) {
                        alert("Đã có lỗi: " + xhr.responseText);
                        $('.loading-container').hide();
                    }
                });
            });

            // Thêm sự kiện cho việc chọn biến thể bằng cách nhấp vào thẻ
            $(document).on('click', '.variant-card', function () {
                if (!$(this).hasClass('out-of-stock')) {
                    $('.variant-card').removeClass('selected');
                    $(this).addClass('selected');

                    // Thêm logic xử lý khi chọn biến thể
                    var selectedVariantId = $(this).data('id');
                    // Có thể gọi API hoặc cập nhật thông tin hiển thị tùy theo yêu cầu của bạn
                    console.log("Đã chọn biến thể ID: " + selectedVariantId);
                }
            });
        });
    });
    // Handle product search
    let timeout = null;
    $('.searchProducts').on('input', function () {
        clearTimeout(timeout);
        const name = $(this).val().trim();
        const cateId = $('select[name="CateId"]').val();
        timeout = setTimeout(function () {
            let url = '/Admin/POS/GetProducts';
            $.ajax({
                url: url,
                data: { name: name, cateId: cateId },
                success: function (res) {
                    if (res.success) {
                        let productsHtml = '';
                        if (res.products.length > 0) {
                            $.each(res.products, function (index, product) {
                                productsHtml += `
                                <div class="col-6 col-md-3 mb-3">
                                    <div class="card h-100">
                                        <a class="getProductInfo" type="button" data-id="${product.productId}">
                                            <img src="/Upload/Products/${product.image}" style="width:135px; height:135px; object-fit: cover; display: block; margin: 0 auto;" class="card-img-top" alt="${product.name}">
                                            <div class="card-body">
                                                <p class="card-text text-truncate-2">${product.name}</p>
                                                <p class="card-text">${product.sellPrice || 'Giá chưa cập nhật'}</p>
                                            </div>
                                        </a>
                                    </div>
                                </div>
                                `;
                            });
                        } else {
                            productsHtml = '<div class="col-12"><p class="text-center">Không tìm thấy sản phẩm nào.</p></div>';
                        }
                        $('#CardProducts .row').html(productsHtml);
                    } else {
                        $('#CardProducts .row').html('<div class="col-12"><p class="text-center">Đã xảy ra lỗi khi tìm kiếm sản phẩm.</p></div>');
                    }
                },
                error: function () {
                    $('.loading-container').hide();
                    $('#CardProducts .row').html('<div class="col-12"><p class="text-center">Đã xảy ra lỗi kết nối.</p></div>');
                }
            });
        }, 300);
    });
    //Hàm lấy các sản phẩm theo Category
    $('select[name="CateId"]').change(function () {
        var id = $(this).val();
        let name = $('input[name="searchProducts"]').val(); // Lấy giá trị từ ô tìm kiếm
        let url = '/Admin/POS/GetProducts';

        $('.loading-container').show();

        $.ajax({
            url: url,
            data: { name: name, cateId: id },
            success: function (res) {
                $('.loading-container').hide();
                if (res.success) {
                    let productsHtml = '';
                    // Duyệt qua từng sản phẩm và tạo HTML tương ứng
                    $.each(res.products, function (index, product) {
                        productsHtml += `
                    <div class="col-6 col-md-3 mb-3">
                        <div class="card h-100">
                            <a class="getProductInfo" type="button" data-id="${product.productId}">
                                <img src="/Upload/Products/${product.image}" style="width:135px; height:135px; object-fit: cover; display: block; margin: 0 auto;" class="card-img-top" alt="...">
                                <div class="card-body">
                                    <p class="card-text text-truncate-2">${product.name}</p>
                                    <p class="card-text">${product.sellPrice || 'Giá chưa cập nhật'}</p>
                                </div>
                            </a>
                        </div>
                    </div>
                `;
                    });

                    // Gán HTML mới vào phần tử CardProducts
                    $('#CardProducts .row').html(productsHtml);
                } else {
                    alert(res.message);
                }
            },
            error: function (xhr, status, error) {
                alert("Đã có lỗi xảy ra: " + xhr.responseText);
                $('.loading-container').hide();
            }
        });
    });
    //Hàm check giá trị của Voucher
    $('#Checkvoucher').click(function () {
        var voucher = $('input[name="Voucher"]').val();
        let url = '/Admin/POS/CheckVoucher';

        if (voucher.length > 3) {
            $.ajax({
                url: url,
                data: { code: voucher }, // Đổi key từ 'voucher' thành 'code' để khớp với action method
                success: function (res) {
                    if (res.success) {
                        $('#resultVoucher').text("Voucher đã được áp dụng thành công").removeClass('text-danger').addClass('text-success').show();

                        //Format thành tiền Việt cho phần giảm giá
                        if (res.voucher.promotion[res.voucher.promotion.length - 1] != "%") {
                            // Nếu là giảm tiền mặt (không có %), chuyển đổi sang số và format thành tiền VND
                            var promotionAmount = parseFloat(res.voucher.promotion.replace(/,/g, '')); // Loại bỏ dấu phẩy (nếu có) và chuyển thành số
                            var formattedPromotion = promotionAmount.toLocaleString() + 'đ';

                            $('#Discount').text(formattedPromotion);
                        } else {
                            // Nếu là phần trăm giảm giá (%)
                            $('#Discount').text(res.voucher.promotion);
                        }
                        updateTotalPayment();

                    } else {
                        $('#resultVoucher').text(res.message).removeClass('text-success').addClass('text-danger').show();
                    }
                },
                error: function (xhr, message) {
                    $('#resultVoucher').text("Có lỗi xảy ra: " + message).removeClass('text-success').addClass('text-danger').show();
                }
            });
        } else {
            $('#resultVoucher').text("Mã voucher không hợp lệ.").removeClass('text-success').addClass('text-danger').show();
        }
    });
    //Hàm lấy thông tin Khách Hàng
    $('select[name="selectCustomer"]').change(function () {
        $('#nameCustomer').text('');
        $('#phoneCustomer').text('');
        $('#addressCustomer').text('');

        var id = $(this).val();
        let url = '/Admin/POS/GetUser';
        $('#Loading-container').show();

        $.ajax({
            url: url,
            data: { id: id },
            success: async function (res) { // Mark the success function as async
                const address = res.addresses[0]; // Access the first address in the array
                const province = await getProvince(address.province); // Use await here
                const district = await getDistrict(address.district);
                const ward = await getWard(address.ward);

                $('#Loading-container').hide();
                $('#nameCustomer').text(res.lastName + " " + res.firstName);
                $('#phoneCustomer').text(res.phoneNumber);
                $('#addressCustomer').text(address.addressLine + ", " + ward.name + ", " + district.name + ", " + province.name); // Access the name property from the API response
            },
            error: function (xhr, status, error) {
                alert("Đã có lỗi xảy ra: " + xhr.responseText);
                $('#Loading-container').hide();
            }
        });
    });
    //Lấy data Province Việt Nam
    async function fetchStaticData() {
        const response = await fetch('/Province_VN.json');
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        return await response.json(); // Chuyển đổi phản hồi thành JSON
    }

    async function getProvince(provinceId) {
        const allData = await fetchStaticData();
        return allData.find(province => province.code === provinceId);
    }

    async function getDistrict(districtId) {
        const allData = await fetchStaticData();
        for (const province of allData) {
            const district = province.districts.find(d => d.code === districtId);
            if (district) {
                return district;
            }
        }
        return null;
    }

    async function getWard(wardId) {
        const allData = await fetchStaticData();
        for (const province of allData) {
            for (const district of province.districts) {
                const ward = district.wards.find(w => w.code === wardId);
                if (ward) {
                    return ward;
                }
            }
        }
        return null;
    }

    //Xử lý sự kiện nút thêm vào giỏ hàng
    $('#AddToCart').click(function () {
        var selectedVariant = $('.variant-card.selected'); // Tìm thẻ được chọn
        var id_variant = selectedVariant.data('id'); // Lấy id_variant từ thẻ được chọn
        var id_product = $('#IdProduct').val();
        $('#productDetail').modal('hide');
        console.log(id_product)
        if (id_variant != null) {
            $.ajax({
                url: '/Admin/POS/GetVarientProduct',
                type: 'GET',
                data: { id: id_variant, productId: id_product }, // Thêm productId vào dữ liệu gửi đi
                success: function (response) {
                    if (response.success) {
                        // Lấy dữ liệu biến thể từ result
                        var varient = response.varient.result;

                        // Tạo HTML cho một dòng mới trong bảng
                        var newRow = `
                                <tr>
                                    <td>${varient.product.name}</td>
                                    <td>${varient.attributes}</td>
                                        <td><input  min="1" type="number" class="form-control quantity-input" value="1" /></td>
                                    <td>${varient.price.toLocaleString()}đ</td>
                                    <td>
                                              <button class="btn btn-danger btn-sm btn-delete"><i class="fas fa-trash"></i></button>
                                    </td>
                                </tr>
                            `;

                        // Append dòng mới vào tbody của bảng
                        $('#shopping-cart tbody').append(newRow);
                        updateTotalPrice();
                    } else {
                        alert(response.message);
                    }

                },
                error: function () {
                    alert("Đã xảy ra lỗi khi lấy biến thể sản phẩm.");
                }
            });
        } else {
            $.ajax({
                url: '/Admin/Pos/GetProduct',
                type: 'GET',
                data: { id: id_product },
                success: function (response) {
                    // Lấy dữ liệu biến thể và thêm vào bảng giỏ hàng

                    // Tạo HTML cho một dòng mới trong bảng
                    var newRow = `
                            <tr>
                                    <td>${response.Product.Name}</td>
                                <td>Không</td>
                                <td><input type="number" min="1" class="form-control" value="1" /></td>
                                    <td>${response.Price.toLocaleString()}đ</td>
                                <td>
                                            <button class="btn btn-danger btn-sm btn-delete">Xóa</button>
                                </td>
                            </tr>
                        `;

                    // Append dòng mới vào tbody của bảng
                    $('#shopping-cart tbody').append(newRow);
                },
                error: function () {
                    alert("Đã xảy ra lỗi khi lấy sản phẩm.");
                }
            });
        }
    });
    // Sự kiện xóa cho nút "Xóa" sử dụng event delegation
    $('#shopping-cart').on('click', '.btn-delete', function () {
        // Xác nhận trước khi xóa
        if (confirm('Bạn có chắc muốn xóa sản phẩm này ra khỏi giỏ hàng không?')) {
            // Xóa hàng hiện tại
            $(this).closest('tr').remove();
            updateTotalPrice();
            updateTotalPayment();
        }
    });

    //Nhập giá tiền trừ trực tiếp
    $('.btn-deduct').on('click', function () {
        // Lấy giá trị từ input DeductValue
        var deductValue = $('input[name="DeductValue"]').val();
        // Chuyển deductValue thành số trước khi định dạng
        var formattedValue = Number(deductValue).toLocaleString('vi-VN');

        // Gán giá trị đã định dạng vào phần tử với id="Deduct"
        $('#Deduct').text(formattedValue + 'đ');


        // Ẩn modal sau khi lưu
        $('#ModalDeduct').modal('hide');
        updateTotalPayment();

    });


    $('.btn-discount').on('click', function () {
        // Lấy giá trị từ input DeductValue
        var discountValue = $('input[name="DiscountValue"]').val();
        // Chuyển deductValue thành số trước khi định dạng
        var formattedValue = Number(discountValue).toLocaleString('vi-VN');
        // Gán giá trị đã định dạng vào phần tử với id="Deduct"
        $('#Discount').text(formattedValue + 'đ');
        // Ẩn modal sau khi lưu
        $('#ModalDiscount').modal('hide');
        //Cập nhật giá tổng
        updateTotalPayment();

    });
    // Sự kiện khi modal deduct bị ẩn
    $('#ModalDeduct').on('hidden.bs.modal', function () {
        $('input[name="DeductValue"]').val(0);
        //Tắt trạng thái load
        $('.loading-container').hide();
    });
    // Sự kiện khi modal discount bị ẩn
    $('#ModalDiscount').on('hidden.bs.modal', function () {
        $('input[name="DiscountValue"]').val(0);
        //Tắt trạng thái load
        $('.loading-container').hide();
    });
    // Sự kiện khi modal Product bị ẩn
    $('#productDetail').on('hidden.bs.modal', function () {
        $('input[name="DeductValue"]').val(0);
        //Tắt trạng thái load
        $('#nameProduct').text('');
        $('#priceProduct').text('');
        $('#categoryName').text('');
        $('#brandName').text('');
        $('#sku').text('')
        $('#imgProduct').attr('src', '/Upload/Products/none.png');
        $('#IdProduct').val('');
        $('.loading-container').hide();
    });

    function updateTotalPrice() {
        // Lấy tất cả các ô có chứa giá tiền
        const priceCells = document.querySelectorAll('td'); // Giả sử tất cả các <td> chứa giá
        let total = 0;

        priceCells.forEach(cell => {
            // Lấy nội dung trong ô giá, loại bỏ dấu phân cách, chữ 'đ', và các ký tự khoảng trắng
            let priceText = cell.textContent.replace(/[.,đ\s]/g, '');
            let priceValue = parseInt(priceText, 10);

            // Kiểm tra nếu là số hợp lệ, thêm vào tổng
            if (!isNaN(priceValue)) {
                total += priceValue;
            }
        });

        // Cập nhật giá trị tổng giá vào phần tử với id="TotalPrice", định dạng lại giá trị thành chuỗi có dấu chấm
        document.getElementById('TotalPrice').textContent = total.toLocaleString() + 'đ';
        updateTotalPayment();

    }

    // Gọi hàm để cập nhật giá khi cần thiết (ví dụ sau khi tải xong hoặc sau khi cập nhật giỏ hàng)

    function updateTotalPayment() {
        // Lấy và chuyển đổi giá trị TotalPrice
        var totalPricetxt = $('#TotalPrice').text().replace(/[.,đ\s]/g, '');
        var totalPrice = parseInt(totalPricetxt, 10);

        // Lấy và chuyển đổi giá trị Deduct
        var deductPricetxt = $('#Deduct').text().replace(/[.,đ\s]/g, '');
        var deductPrice = parseInt(deductPricetxt, 10);

        // Lấy giá trị Discount
        var discountPricetxt = $('#Discount').text().trim(); // Loại bỏ khoảng trắng đầu và cuối
        var discountPrice = 0;

        // Kiểm tra nếu discount có giá trị trước khi xử lý
        if (discountPricetxt) {
            // Kiểm tra nếu discount là phần trăm hay số tiền
            if (discountPricetxt.includes('%')) {
                // Nếu là phần trăm, lấy số và tính phần trăm từ totalPrice
                var percent = parseFloat(discountPricetxt.replace('%', '')) / 100;
                if (!isNaN(percent)) {
                    discountPrice = totalPrice * percent;
                }
                console.log("%")
            } else {
                // Nếu là số tiền, loại bỏ các ký tự không phải số và chuyển thành số nguyên
                discountPrice = parseInt(discountPricetxt.replace(/[.,đ\s]/g, ''), 10);
                console.log("đ")
            }
        }


        // Tính tổng tiền thanh toán
        var total = totalPrice - deductPrice - discountPrice;

        // Hiển thị tổng tiền thanh toán với định dạng có dấu chấm ngăn cách
        $('#TotalPayment').text(total.toLocaleString() + 'đ');
    }

});
