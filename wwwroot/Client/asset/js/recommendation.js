/**
 * RecommendationManager - Quản lý gợi ý sản phẩm cho toàn website
 */
const RecommendationManager = {
    // 1. Cấu hình chung
    config: {
        currencyLocale: 'vi-VN',
        uploadPath: '/Upload/Products/',
        defaultContainer: '#recommendation-container'
    },

    // 2. Hàm khởi tạo chính cho từng Scene
    init: function (scene, productSysId = null, topN = 15) {
        if (typeof productSysId === 'number' && topN === 15 && scene !== 'detail') {
            topN = productSysId;
            productSysId = null;
        }
        this.loadData(scene, productSysId, topN);
    },

    // 3. Xử lý gọi API
    loadData: function (scene, productSysId, topN) {
        const _self = this;
        const request = this.helper.buildRequest(scene, productSysId, topN);

        if (!request) {
            console.error("Recommend Error: unsupported scene", scene);
            $(_self.config.defaultContainer).hide();
            return;
        }

        $.ajax({
            url: request.url,
            type: 'GET',
            data: request.data,
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
        $container.closest('section.products').show();

        // Sau khi render xong thì gắn sự kiện
        this.bindEvents();

        const wrapperKey = $container.closest('[data-wrapper]').data('wrapper');
        this.refreshScrollState(wrapperKey, $container);
    },

    refreshScrollState: function (wrapperKey, $container) {
        const forceRecommendationButtons = () => {
            const wrapper = $container.closest('.scrolling-wrapper').get(0);
            if (!wrapper) {
                return;
            }

            const section = wrapper.closest('.product-section') || document;
            const targetKey = wrapper.dataset.wrapper || wrapperKey || 'suggestion';
            const btnPrev = section.querySelector(`.btn-prev[data-target="${targetKey}"]`) || document.querySelector(`.btn-prev[data-target="${targetKey}"]`);
            const btnNext = section.querySelector(`.btn-next[data-target="${targetKey}"]`) || document.querySelector(`.btn-next[data-target="${targetKey}"]`);
            const cards = wrapper.querySelectorAll('.product-card');

            if (!btnPrev || !btnNext || cards.length === 0 || window.innerWidth < 768) {
                return;
            }

            const wrapperRect = wrapper.getBoundingClientRect();
            const firstCardRect = cards[0].getBoundingClientRect();
            const lastCardRect = cards[cards.length - 1].getBoundingClientRect();
            const contentWidth = Math.max(0, lastCardRect.right - firstCardRect.left);
            const hasOverflow = contentWidth > wrapperRect.width + 8;
            const hasHiddenLeft = wrapper.scrollLeft > 5 || firstCardRect.left < wrapperRect.left - 4;
            const hasHiddenRight = hasOverflow && (lastCardRect.right > wrapperRect.right + 4 || wrapper.scrollLeft <= 5);

            btnPrev.classList.toggle('show', hasOverflow && hasHiddenLeft);
            btnNext.classList.toggle('show', hasOverflow && hasHiddenRight);
        };

        const dispatchRefresh = () => {
            forceRecommendationButtons();

            if (window.ScrollSectionManager) {
                window.ScrollSectionManager.init(document);
                window.ScrollSectionManager.refresh(document);
            }

            forceRecommendationButtons();

            window.dispatchEvent(new CustomEvent('recommendation:rendered', {
                detail: {
                    wrapper: wrapperKey || null
                }
            }));

            forceRecommendationButtons();
        };

        requestAnimationFrame(() => {
            requestAnimationFrame(dispatchRefresh);
        });

        setTimeout(dispatchRefresh, 120);
        setTimeout(dispatchRefresh, 320);
        setTimeout(dispatchRefresh, 1000);

        const pathname = (window.location.pathname || '').toLowerCase();
        const isHomepage = pathname === '/' || pathname === '/home' || pathname === '/home/index';

        if (isHomepage) {
            window.addEventListener('load', dispatchRefresh, { once: true });
            setTimeout(dispatchRefresh, 1600);
        }

        $container.find('img').each(function () {
            if (this.complete) {
                return;
            }

            $(this).one('load error', dispatchRefresh);
        });
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
                    <div class="product-link recommendation-product-link" style="cursor:pointer" data-slug="${p.slug}" data-product-id="${p.productId}" data-product-sys-id="${p.productSysId || ''}" data-track-event="recommendation_click" data-track-source="recommendation_widget" data-track-placement="recommendation">
                        <div class="image-container">
                            <img src="${img}" class="product-image-csl-hotSale" alt="${p.name}">
                        </div>
                        <div class="card-body p-3 d-flex flex-column">
                            <h6 class="card-title mb-2 product-title" style="font-size: 0.9rem; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden;">
                                ${p.name}
                            </h6>
                            <div class="price-wrapper d-flex align-items-center text-center gap-1 mb-2 flex-wrap" style="min-height: 3rem;">
                                <div class="price-text text-danger fw-bold" style="font-size: 1rem;">${price}</div>
                                ${oldPrice ? `<div class="text-decoration-line-through text-muted text-end" style="font-size: 0.85rem;">${oldPrice}</div>` : ''}
                            </div>
                            <button class="btn btn-outline-danger btn-sm w-100 mt-auto addToWishList" data-id="${p.productId}">
                                <i class="far fa-heart pe-1"></i><span>Yêu thích</span>
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
        buildRequest: function (scene, productSysId, topN) {
            const normalizedTopN = Number.isFinite(Number(topN)) ? Number(topN) : 15;

            if (scene === 'homepage') {
                return {
                    url: '/api/v1/recommendations/homepage',
                    data: { topN: normalizedTopN }
                };
            }

            if (scene === 'detail' || scene === 'cart' || scene === 'wishlist') {
                return {
                    url: '/api/v1/recommendations/scene',
                    data: {
                        scene,
                        productSysId,
                        topN: normalizedTopN
                    }
                };
            }

            return null;
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
        const $container = $(this.config.defaultContainer);

        // Sự kiện click chuyển trang
        $container.find('.recommendation-product-link').off('click').on('click', function (e) {
            if ($(e.target).closest('.addToWishList').length) {
                e.preventDefault();
                e.stopPropagation();
                return;
            }

            const slug = $(this).data('slug');
            if (window.UserInteractionTracker) {
                window.UserInteractionTracker.trackElement(this);
            }
            window.location.href = `/view/${slug}`;
        });

        // Sự kiện nút yêu thích
        $container.find('.addToWishList').off('click').on('click', function (e) {
            e.stopPropagation();
            const id = $(this).data('id');
            if (typeof addToWishlist === 'function') {
                addToWishlist(id); // Gọi hàm global có sẵn của bạn
            }
        });

        if (typeof window.syncWishlistCards === 'function') {
            window.syncWishlistCards();
        }
    }
};
