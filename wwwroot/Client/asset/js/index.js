(function () {
    function getScrollButtons(slider) {
        const key = slider.getAttribute('data-wrapper');
        const section = slider.closest('.product-section') || document;

        let btnPrev = section.querySelector(`.btn-prev[data-target="${key}"]`);
        let btnNext = section.querySelector(`.btn-next[data-target="${key}"]`);

        if (!btnPrev || !btnNext) {
            btnPrev = document.querySelector(`.btn-prev[data-target="${key}"]`);
            btnNext = document.querySelector(`.btn-next[data-target="${key}"]`);
        }

        return { btnPrev, btnNext };
    }

    function updateButtons(slider) {
        if (!slider) {
            return;
        }

        const { btnPrev, btnNext } = getScrollButtons(slider);
        if (!btnPrev || !btnNext) {
            return;
        }

        if (window.innerWidth < 768) {
            btnPrev.classList.remove('show');
            btnNext.classList.remove('show');
            return;
        }

        const cards = slider.querySelectorAll('.product-card');
        const sliderRect = slider.getBoundingClientRect();
        const scrollWidth = slider.scrollWidth;
        const clientWidth = slider.clientWidth;
        const scrollLeft = Math.ceil(slider.scrollLeft);
        const maxScrollLeft = Math.max(0, scrollWidth - clientWidth);

        let hasHiddenLeft = scrollLeft > 5;
        let hasHiddenRight = scrollLeft < maxScrollLeft - 5;

        if (cards.length > 0) {
            const firstCardRect = cards[0].getBoundingClientRect();
            const lastCardRect = cards[cards.length - 1].getBoundingClientRect();

            hasHiddenLeft = hasHiddenLeft || firstCardRect.left < sliderRect.left - 4;
            hasHiddenRight = hasHiddenRight || lastCardRect.right > sliderRect.right + 4;
        }

        const hasOverflow = hasHiddenLeft || hasHiddenRight || scrollWidth > clientWidth + 8;

        btnPrev.classList.toggle('show', hasOverflow && hasHiddenLeft);
        btnNext.classList.toggle('show', hasOverflow && hasHiddenRight);
    }

    function bindSlider(slider) {
        if (!slider) {
            return;
        }

        if (slider.dataset.scrollBound === 'true') {
            updateButtons(slider);
            return;
        }

        const { btnPrev, btnNext } = getScrollButtons(slider);
        slider.dataset.scrollBound = 'true';

        slider.addEventListener('scroll', function () {
            updateButtons(slider);
        });

        if (btnPrev) {
            btnPrev.addEventListener('click', function () {
                slider.scrollBy({ left: -slider.clientWidth * 0.7, behavior: 'smooth' });
            });
        }

        if (btnNext) {
            btnNext.addEventListener('click', function () {
                slider.scrollBy({ left: slider.clientWidth * 0.7, behavior: 'smooth' });
            });
        }

        if (typeof ResizeObserver !== 'undefined') {
            const observer = new ResizeObserver(function () {
                updateButtons(slider);
            });

            observer.observe(slider);
            if (slider.firstElementChild) {
                observer.observe(slider.firstElementChild);
            }
        }

        requestAnimationFrame(function () {
            updateButtons(slider);
        });

        setTimeout(function () {
            updateButtons(slider);
        }, 200);
    }

    function initScrollableSections(root) {
        const scope = root instanceof Element || root instanceof Document ? root : document;
        scope.querySelectorAll('.scrolling-wrapper').forEach(bindSlider);
    }

    function refreshScrollableSections(root) {
        const scope = root instanceof Element || root instanceof Document ? root : document;
        scope.querySelectorAll('.scrolling-wrapper').forEach(updateButtons);
    }

    window.ScrollSectionManager = {
        init: initScrollableSections,
        refresh: refreshScrollableSections
    };

    document.addEventListener('DOMContentLoaded', function () {
        initScrollableSections(document);
    });

    window.addEventListener('resize', function () {
        refreshScrollableSections(document);
    });

    window.addEventListener('recommendation:rendered', function (event) {
        const wrapperKey = event.detail && event.detail.wrapper;
        if (!wrapperKey) {
            refreshScrollableSections(document);
            return;
        }

        const wrapper = document.querySelector(`.scrolling-wrapper[data-wrapper="${wrapperKey}"]`);
        if (wrapper) {
            bindSlider(wrapper);
            updateButtons(wrapper);
        }
    });
})();

// Script để hiển thị/ẩn nút
window.onscroll = function () {
    const button = document.getElementById('back-to-top');
    if (!button) {
        return;
    }

    if (document.body.scrollTop > 60 || document.documentElement.scrollTop > 60) {
        button.style.display = 'block';
    } else {
        button.style.display = 'none';
    }
};

// Xử lý sự kiện click
const backToTopButton = document.getElementById('back-to-top');
if (backToTopButton) {
    backToTopButton.addEventListener('click', function () {
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    });
}
