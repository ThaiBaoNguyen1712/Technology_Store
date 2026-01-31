document.querySelectorAll('.btn-scroll').forEach(button => {
    button.addEventListener('click', function() {
      const section = this.dataset.target;
      const wrapper = document.querySelector(`[data-wrapper="${section}"]`);
      const scrollAmount = wrapper.clientWidth / 2;

      if (this.classList.contains('btn-next')) {
        wrapper.scrollBy({ left: scrollAmount, behavior: 'smooth' });
      } else {
        wrapper.scrollBy({ left: -scrollAmount, behavior: 'smooth' });
      }
    });
});
  
document.addEventListener('DOMContentLoaded', function () {
    const sliders = document.querySelectorAll('.scrolling-wrapper');

    sliders.forEach(slider => {
        // Lấy key từ data-wrapper theo đúng rule của bạn
        const key = slider.getAttribute('data-wrapper');

        // Tìm nút có data-target khớp với data-wrapper
        const btnPrev = document.querySelector(`.btn-prev[data-target="${key}"]`);
        const btnNext = document.querySelector(`.btn-next[data-target="${key}"]`);

        if (!btnPrev || !btnNext) return;

        function updateButtons() {

            if (window.innerWidth < 768) {
                btnPrev.classList.remove('show');
                btnNext.classList.remove('show');
                return;
            }

            const scrollWidth = slider.scrollWidth;
            const clientWidth = slider.clientWidth;
            const scrollLeft = Math.ceil(slider.scrollLeft);
            const maxScrollLeft = scrollWidth - clientWidth;

            // Hiện nút quay lại khi đã cuộn đi một chút
            if (scrollLeft > 5) {
                btnPrev.classList.add('show');
            } else {
                btnPrev.classList.remove('show');
            }

            // Hiện nút tiếp theo nếu nội dung còn dài và chưa cuộn hết
            if (scrollWidth > clientWidth && scrollLeft < maxScrollLeft - 5) {
                btnNext.classList.add('show');
            } else {
                btnNext.classList.remove('show');
            }
        }

        slider.addEventListener('scroll', updateButtons);
        window.addEventListener('resize', updateButtons);

        // Kiểm tra ngay khi trang tải xong
        setTimeout(updateButtons, 500);

        btnPrev.addEventListener('click', () => {
            slider.scrollBy({ left: -slider.clientWidth * 0.7, behavior: 'smooth' });
        });

        btnNext.addEventListener('click', () => {
            slider.scrollBy({ left: slider.clientWidth * 0.7, behavior: 'smooth' });
        });
    });
});

// Script để hiển thị/ẩn nút
window.onscroll = function () {
    if (document.body.scrollTop > 60 || document.documentElement.scrollTop > 60) {
        document.getElementById("back-to-top").style.display = "block";
    } else {
        document.getElementById("back-to-top").style.display = "none";
    }
};

// Xử lý sự kiện click
document.getElementById('back-to-top').addEventListener('click', function () {
    window.scrollTo({
        top: 0,
        behavior: 'smooth'
    });
});
