
function loadNotifications() {
    window.isLoadingNotifications = true; 
    $.getJSON(`/notifications/GetUserNotifications`, function (data) {
        const notificationList = $(".notification-list"); // Trỏ tới danh sách thông báo
        notificationList.empty(); // Xóa hết thông báo cũ

        // Cập nhật badge số lượng thông báo chưa đọc
        var $notificationBadge = $('.notification'); // Chọn phần tử hiển thị số thông báo
        var currentCount = parseInt($notificationBadge.text(), 10); // Lấy số lượng hiện tại
        if (!isNaN(currentCount) && currentCount > 0) {
            $notificationBadge.text(currentCount + 1); // Tăng số lượng lên 1
        }
        // Nếu có thông báo, hiển thị danh sách
        if (data.length > 0) {
            data.forEach(n => {
                const icon = getNotificationIcon(n.type);
                const item = `
                    <a href="${n.redirectUrl}" class="notification-item ${n.isRead ? 'bg-white' : 'bg-light mark-as-read'}" data-id="${n.userNotificationId}">
                        <div class="notification-content">
                            <div class="notification-avatar">
                                <div class="notif-icon ${icon.color}">
                                    <i class="fa ${icon.icon}"></i>
                                </div>
                            </div>
                            <div class="notification-info">
                                <p class="notification-text">${n.title}</p>
                                <span class="block">
                                    <small class="text-truncated" style="max-width:400px" title="${n.message}">
                                        ${n.message.length > 50 ? n.message.substring(0, 50) + "..." : n.message}
                                    </small>
                                </span>
                                <span class="notification-time">${timeAgo(n.createdAt)}</span>
                            </div>
                        </div>
                    </a>`;
                notificationList.append(item);
            });
        } else {
            // Nếu không có thông báo
            const noNotif = `
                <div class="notification-content" style="min-height:40px">
                    <span class="block d-flex text-center justify-content-center align-items-center">
                        Không có thông báo
                    </span>
                </div>`;
            notificationList.append(noNotif);
        }
        window.isLoadingNotifications = false; // Reset lại khi load xong
    });
}

// Hàm lấy icon theo loại thông báo
function getNotificationIcon(type) {
    const icons = {
        "new register": { icon: "fa-user-plus", color: "text-primary" },
        "order completed": { icon: "fa-check-circle", color: "text-success" },
        "low stock": { icon: "fa-exclamation-triangle", color: "text-warning" },
        "warning": { icon: "fa-exclamation-triangle", color: "text-warning" },
        "error": { icon: "fa-times-circle", color: "text-danger" },
        "danger": { icon: "fa-times-circle", color: "text-danger" },
        "new comment": { icon: "fa-comment", color: "text-success" },
        "new order": { icon: "fa-shopping-cart", color: "text-primary" },
        "payment received": { icon: "fa-credit-card", color: "text-success" },
        "success": { icon: "fa-credit-card", color: "text-success" },
        "product added": { icon: "fa-plus", color: "text-info" },
        "info": { icon: "fa-plus", color: "text-info" },
        "shipment sent": { icon: "fa-truck", color: "text-warning" },
        "account updated": { icon: "fa-user-edit", color: "text-secondary" },
        "system update": { icon: "fa-wrench", color: "text-secondary" }
    };
    return icons[type] || { icon: "fa-info-circle", color: "text-secondary" };
}

function getNotificationTitle(type) {
    switch (type) {
        case "new register":
            return "Có người đăng ký mới";
        case "order completed":
            return "Đơn hàng hoàn tất";
        case "low stock":
            return "Số lượng tồn kho thấp";
        case "error":
            return "Lỗi hệ thống";
        case "new comment":
            return "Bình luận mới";
        case "new order":
            return "Đơn hàng mới";
        case "payment received":
            return "Đã nhận thanh toán";
        case "product added":
            return "Sản phẩm mới được thêm";
        case "shipment sent":
            return "Giao hàng đã được gửi";
        case "account updated":
            return "Tài khoản đã được cập nhật";
        case "system update":
            return "Cập nhật hệ thống";
        default:
            return "Thông báo";
    }
}


// Hàm hỗ trợ chuyển đổi thời gian thành "Vừa xong", "1 phút trước", ...
function timeAgo(dateString) {
    const now = new Date();
    const past = new Date(dateString);
    const diff = Math.floor((now - past) / 1000); // Tính thời gian chênh lệch theo giây

    if (diff < 60) return "Vừa xong";
    if (diff < 3600) return `${Math.floor(diff / 60)} phút trước`;
    if (diff < 86400) return `${Math.floor(diff / 3600)} giờ trước`;
    return `${Math.floor(diff / 86400)} ngày trước`;
}

// Hàm hiển thị toast
function showToast(title, message, redirectUrl) {
    $("#toastTitle").text(title);
    $("#toastBody").text(message);
    $("#toastTime").text("Vừa xong");
    $("#redirectUrl").attr("href", redirectUrl);

    const toast = new bootstrap.Toast(document.getElementById("liveToast"));
    toast.show();
}
function playNotificationSound() {
    var audio = new Audio('/sounds/notification.mp3');
    audio.play().catch(() => {
        console.log("Không thể phát âm thanh mp3, thử phát file dự phòng.");
        var fallbackAudio = new Audio('/sounds/notification.ogg');
        fallbackAudio.play().catch(() => {
            console.log("Không thể phát âm thanh.");
        });
    });
}

$("#markAllAsReadClient").click(function () {
    $.post(`/notifications/mark-all-as-read/${userId}`, function () {
        loadNotifications();
    });
});
// Kết nối SignalR
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .build();

connection.on("ReceiveNotification", function (notification) {
    // Hiển thị thông báo mới trong danh sách
    loadNotifications();

    // Phát âm thanh thông báo
    playNotificationSound();

    // Hiển thị thông báo trong Toast
    const title = getNotificationTitle(notification.type) || "Thông báo mới";
    showToast(title, notification.message, notification.redirectUrl);

    // Thay đổi tiêu đề của tab
    const originalTitle = document.title; // Lưu tiêu đề gốc
    document.title = `${title} - ${notification.message}`; // Đặt tiêu đề mới

    // Khôi phục tiêu đề ban đầu sau khi người dùng tương tác
    window.addEventListener('focus', function () {
        document.title = originalTitle;
    }, { once: true }); // Chỉ lắng nghe một lần
});

// Quay lại tiêu đề ban đầu khi người dùng tương tác
var originalTitle = document.title;

connection.start().then(() => console.log("SignalR Connected"));

