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
        let isDown = false;
        let startX;
        let startY;
        let scrollLeft;

        // Touch events
        slider.addEventListener('touchstart', (e) => {
            isDown = true;
            slider.style.scrollBehavior = 'smooth'; // Tắt smooth để phản hồi nhanh
            startX = e.touches[0].pageX - slider.offsetLeft;
            startY = e.touches[0].pageY;
            scrollLeft = slider.scrollLeft;
        }, { passive: false });

        slider.addEventListener('touchend', () => {
            isDown = false;
            slider.style.scrollBehavior = 'smooth'; // Bật lại smooth khi thả
        }, { passive: true });

        slider.addEventListener('touchmove', (e) => {
            if (!isDown) return;

            const x = e.touches[0].pageX - slider.offsetLeft;
            const y = e.touches[0].pageY;
            const walkX = (x - startX) * 1.5; // Giảm tốc độ từ 1.5 xuống 0.8
            const walkY = y - startY;

            // Kiểm tra ý định cuộn: ngang hay dọc
            if (Math.abs(walkX) > Math.abs(walkY)) {
                e.preventDefault(); // Ngăn cuộn dọc khi vuốt ngang
                slider.scrollLeft = scrollLeft - walkX;
            } else {
                isDown = false; // Cho phép cuộn dọc trang
                return;
            }
        }, { passive: false });

        // Mouse events (điều chỉnh tốc độ cho desktop)
        slider.addEventListener('mousedown', (e) => {
            isDown = true;
            slider.style.scrollBehavior = 'auto';
            startX = e.pageX - slider.offsetLeft;
            scrollLeft = slider.scrollLeft;
        });

        slider.addEventListener('mouseleave', () => {
            isDown = false;
        });

        slider.addEventListener('mouseup', () => {
            isDown = false;
            slider.style.scrollBehavior = 'smooth';
        });

        slider.addEventListener('mousemove', (e) => {
            if (!isDown) return;
            e.preventDefault();
            const x = e.pageX - slider.offsetLeft;
            const walk = (x - startX) * 0.8; // Giảm tốc độ từ 1.5 xuống 0.8
            slider.scrollLeft = scrollLeft - walk;
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
  ////Phần Danh Mục
  //$(document).ready(function() {
  //  // Add hover effect for dropdown items
  //  $('.dropdown-item').hover(
  //    function() {
  //      $(this).find('.fa-angle-right').css('transform', 'translateX(5px)');
  //    },
  //    function() {
  //      $(this).find('.fa-angle-right').css('transform', 'translateX(0)');
  //    }
  //  );
  
  //  // Optional: Add click handling for mobile
  //  $('.category-link').on('click', function(e) {
  //    if (window.innerWidth < 768) {
  //      e.preventDefault();
  //      $(this).next('.dropdown-menu').slideToggle();
  //    }
  //  });
  //});
  