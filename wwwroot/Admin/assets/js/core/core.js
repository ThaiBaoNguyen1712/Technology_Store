function removeVietnameseTones(str) {
    return str
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '') // bỏ dấu
        .replace(/đ/g, 'd')
        .replace(/Đ/g, 'D')
        .replace(/\s+/g, '-')           // space -> -
        .toLowerCase();
}