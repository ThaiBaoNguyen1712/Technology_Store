/**
 * RecommendationManager - Quản lý gợi ý sản phẩm cho toàn website
 */
const RecommendationManager = {
    // 1. Cấu hình chung
    config: {
        apiUrl: '/api/v1/recommendations/scene',
        currencyLocale: 'vi-VN',
        uploadPath: '/Upload/Products/',
        defaultContainer: '#recommendation-container'
    },

    // 2. Hàm khởi tạo chính cho từng Scene
    init: function (scene, productSysId = null, topN = 10) {
        this.loadData(scene, productSysId, topN);
    },

    // 3. Xử lý gọi API
    loadData: function (scene, productSysId, topN) {
        const _self = this;
        $.ajax({
            url: this.config.apiUrl,
            type: 'GET',
            data: { scene, productSysId, topN },
            beforeSend: function () {
                $(_self.config.defaultContainer).html(_self.helper.getSpinner());
            },
            success: function (res) {
                if (res && res.products) {
                    _self.renderList(res.products);
                }
            },
            error: function (err) {
                console.error("Recommend Error:", err);
                $(_self.config.defaultContainer).hide();
            }
        });
    },

    // 4. Render danh sách sản phẩm
    renderList: function (products) {
        const $container = $(this.config.defaultContainer);
        $container.empty();

        if (products.length === 0) {
            $container.closest('section.products').hide();
            return;
        }

        const html = products.map(p => this.template.productCard(p)).join('');
        $container.append(html);

        // Sau khi render xong thì gắn sự kiện
        this.bindEvents();
    },

    // 5. Các Template HTML nhỏ (Tách nhỏ để dễ sửa UI)
    template: {
        productCard: function (p) {
            const helper = RecommendationManager.helper;
            const img = helper.formatImage(p.image);
            const price = helper.formatMoney(p.sellPrice);
            const oldPrice = p.sellPrice !== p.originalPrice ? helper.formatMoney(p.originalPrice) : '';
            const badge = helper.getBadge(p);

            return `
                <div class="col-6 col-md-3 card product-card" style="max-width: 240px;" data-id="${p.productId}">
                    <div class="position-absolute d-flex gap-1" style="top: 8px; left: 8px; z-index: 2;">
                        ${badge}
                    </div>
                    <div class="product-link" style="cursor:pointer" data-slug="${p.slug}">
                        <div class="image-container">
                            <img src="${img}" class="product-image-csl-hotSale" alt="${p.name}">
                        </div>
                        <div class="card-body p-3 d-flex flex-column">
                            <h6 class="card-title mb-2 product-title" style="font-size: 0.9rem; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden;">
                                ${p.name}
                            </h6>
                            <div class="d-flex align-items-center text-center gap-1 mb-2 flex-wrap" style="min-height: 3rem;">
                                <div class="text-danger fw-bold" style="font-size: 1rem;">${price}</div>
                                ${oldPrice ? `<div class="text-decoration-line-through text-muted text-end" style="font-size: 0.85rem;">${oldPrice}</div>` : ''}
                            </div>
                            <button class="btn btn-outline-danger btn-sm w-100 mt-auto addToWishList" data-id="${p.productId}">
                                <i class="far fa-heart pe-1"></i> Yêu thích
                            </button>
                        </div>
                    </div>
                </div>`;
        }
    },

    // 6. Các hàm bổ trợ (Helpers)
    helper: {
        formatMoney: function (amount) {
            return amount.toLocaleString('vi-VN', { style: 'currency', currency: 'VND' });
        },
        formatImage: function (imgName) {
            if (!imgName) return '/images/no-image.png';
            return imgName.includes('http') ? imgName : RecommendationManager.config.uploadPath + imgName;
        },
        getBadge: function (p) {
            if (p.sellPrice === p.originalPrice) return '';
            if (p.discountPercentage) return `<span class="badge bg-danger">Giảm ${p.discountPercentage}%</span>`;
            return `<span class="badge bg-danger">Khuyến mãi</span>`;
        },
        getSpinner: function () {
            return `<div class="text-center w-100 py-4"><div class="spinner-border text-danger"></div></div>`;
        }
    },

    // 7. Quản lý sự kiện (Click, Hover...)
    bindEvents: function () {
        // Sự kiện click chuyển trang
        $('.product-link').off('click').on('click', function () {
            const slug = $(this).data('slug');
            window.location.href = `/view/${slug}`;
        });

        // Sự kiện nút yêu thích
        $('.addToWishList').off('click').on('click', function (e) {
            e.stopPropagation();
            const id = $(this).data('id');
            if (typeof addToWishlist === 'function') {
                addToWishlist(id); // Gọi hàm global có sẵn của bạn
            }
        });
    }
};