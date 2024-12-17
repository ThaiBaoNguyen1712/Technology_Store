function loadNotifications() {
    $.getJSON(`/admin/notifications`, function (data) {
        const notificationList = $(".notif-center"); // Trỏ tới danh sách thông báo
        const notificationCount = $(".notification"); // Badge thông báo
        notificationList.empty(); // Xóa hết các thông báo hiện tại trong danh sách

        // Cập nhật badge số lượng thông báo chưa đọc
        const unreadCount = data.filter(n => !n.isRead).length;
        notificationCount.text(unreadCount > 0 ? unreadCount : ""); // Hiển thị số lượng thông báo chưa đọc

        // Thêm thông báo vào danh sách
        if (data.length > 0) {
            data.forEach(n => {
                const icon = getNotificationIcon(n.type); // Lấy biểu tượng và kiểu thông báo
                const item = `
                    <a href="${n.redirectUrl}" class="${n.isRead ? 'read' : 'unread'}">
                        <div class="notif-icon ${icon.color}">
                            <i class="fa ${icon.icon}"></i>
                        </div>
                        <div class="notif-content">
                            <span class="block">
                                <small class="text-truncated" style="max-width:200px" title="${n.message}">
                                    ${n.message.length > 30 ? n.message.substring(0, 30) + "..." : n.message}
                                </small>
                            </span>
                            <span class="time">${timeAgo(n.createdAt)}</span>
                        </div>
                    </a>`;
                notificationList.append(item); // Thêm thông báo vào danh sách
            });
        } else {
            const noNotif = `
                <div class="notif-content" style="min-height:40px">
                    <span class="block d-flex text-center justify-content-center align-items-center">
                        Không có thông báo
                    </span>
                </div>`;
            notificationList.append(noNotif); // Thêm thông báo không có nội dung vào danh sách
        }
    });
}

// Hàm lấy icon và màu sắc theo loại thông báo
function getNotificationIcon(type) {
    switch (type) {
        case "new register":
            return { icon: "fa-user-plus", color: "notif-primary" };
        case "order completed":
            return { icon: "fa-check-circle", color: "notif-success" };
        case "low stock":
            return { icon: "fa-exclamation-triangle", color: "notif-warning" };
        case "error":
            return { icon: "fa-times-circle", color: "notif-danger" };
        case "new comment":
            return { icon: "fa-comment", color: "notif-success" }; // Màu xanh cho "new comment"
        case "new order":
            return { icon: "fa-shopping-cart", color: "notif-primary" };
        case "payment received":
            return { icon: "fa-credit-card", color: "notif-success" };
        case "product added":
            return { icon: "fa-plus", color: "notif-info" };
        case "shipment sent":
            return { icon: "fa-truck", color: "notif-warning" };
        case "account updated":
            return { icon: "fa-user-edit", color: "notif-secondary" };
        case "system update":
            return { icon: "fa-wrench", color: "notif-secondary" };
        default:
            return { icon: "fa-info-circle", color: "notif-secondary" };
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
function showToast(title, message,redirectUrl) {
    $("#toastTitle").text(title);
    $("#toastBody").text(message);
    $("#toastTime").text("Vừa xong");
    $("#redirectUrl").attr("href", redirectUrl);

    const toast = new bootstrap.Toast(document.getElementById("liveToast"));
    toast.show();
}

$("#markAllAsRead").click(function () {
    $.post(`/admin/notifications/mark-all-as-read/${userId}`, function () {
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

    // Hiển thị thông báo trong Toast
    showToast("Thông báo mới", notification.message, notification.redirectUrl);
});

connection.start().then(() => console.log("SignalR Connected"));
