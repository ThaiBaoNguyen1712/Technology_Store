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
        let scrollLeft;

        // Touch events
        slider.addEventListener('touchstart', (e) => {
            isDown = true;
            slider.style.scrollBehavior = 'smooth';
            startX = e.touches[0].pageX - slider.offsetLeft;
            scrollLeft = slider.scrollLeft;
        });

        slider.addEventListener('touchend', () => {
            isDown = false;
            slider.style.scrollBehavior = 'smooth';
        });

        slider.addEventListener('touchmove', (e) => {
            if (!isDown) return;
            e.preventDefault();
            const x = e.touches[0].pageX - slider.offsetLeft;
            const walk = (x - startX) * 1.5; // Scroll speed multiplier
            slider.scrollLeft = scrollLeft - walk;
        });

        // Mouse events (optional, for desktop drag scrolling)
        slider.addEventListener('mousedown', (e) => {
            isDown = true;
            slider.style.scrollBehavior = 'smooth';
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
            const walk = (x - startX) * 1.5;
            slider.scrollLeft = scrollLeft - walk;
        });
    });
});
// Script để hiển thị/ẩn nút
window.onscroll = function () {
    if (document.body.scrollTop > 20 || document.documentElement.scrollTop > 20) {
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
  //Phần Danh Mục
  $(document).ready(function() {
    // Add hover effect for dropdown items
    $('.dropdown-item').hover(
      function() {
        $(this).find('.fa-angle-right').css('transform', 'translateX(5px)');
      },
      function() {
        $(this).find('.fa-angle-right').css('transform', 'translateX(0)');
      }
    );
  
    // Optional: Add click handling for mobile
    $('.category-link').on('click', function(e) {
      if (window.innerWidth < 768) {
        e.preventDefault();
        $(this).next('.dropdown-menu').slideToggle();
      }
    });
  });
  