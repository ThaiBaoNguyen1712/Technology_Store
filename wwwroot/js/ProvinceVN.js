const fetchData = async () => {
    const response = await fetch('/Province_VN.json');
    if (!response.ok) {
        throw new Error('Network response was not ok');
    }
    return response.json();
};

const populateSelect = (selectElement, data, textKey = 'full_name', valueKey = 'code') => {
    selectElement.innerHTML = '';
    data.forEach(item => {
        const option = document.createElement('option');
        option.value = item[valueKey];  // Chỉ sử dụng code
        option.textContent = item[textKey];  // Chỉ sử dụng full_name
        selectElement.appendChild(option);
    });
    // Cập nhật Bootstrap Select sau khi thay đổi nội dung
    $(selectElement).selectpicker('refresh');
};

// Lấy các phần tử DOM
const citySelect = document.getElementById('citySelect');
const districtSelect = document.getElementById('districtSelect');
const wardSelect = document.getElementById('wardSelect');

// Khởi tạo
let allData = [];
let cities = [];
let districts = [];
let wards = [];

// Tải toàn bộ dữ liệu
const loadAllData = async () => {
    allData = await fetchData();
    cities = allData;
    populateSelect(citySelect, cities);
};

// Xử lý sự kiện khi chọn tỉnh/thành phố
citySelect.addEventListener('change', function () {
    const selectedCityCode = this.value;
    if (selectedCityCode) {
        const selectedCity = cities.find(city => city.code === selectedCityCode);
        districts = selectedCity.districts || [];
        populateSelect(districtSelect, districts);
        $(wardSelect).empty().selectpicker('refresh');
    } else {
        $(districtSelect).empty().selectpicker('refresh');
        $(wardSelect).empty().selectpicker('refresh');
    }
});

// Xử lý sự kiện khi chọn quận/huyện
districtSelect.addEventListener('change', function () {
    const selectedDistrictCode = this.value;
    if (selectedDistrictCode) {
        const selectedDistrict = districts.find(district => district.code === selectedDistrictCode);
        wards = selectedDistrict.wards || [];
        populateSelect(wardSelect, wards);
    } else {
        $(wardSelect).empty().selectpicker('refresh');
    }
});

// Xử lý sự kiện khi chọn phường/xã
wardSelect.addEventListener('change', function () {
    const selectedWardId = this.options[this.selectedIndex].value; // Chọn code
    console.log('Selected Ward Code:', selectedWardId);
});

// Khởi tạo khi trang được tải
document.addEventListener('DOMContentLoaded', function () {
    $('.selectpicker').selectpicker({
        liveSearch: true,
        size: 10
    });
    loadAllData();
});

async function fillAddressData(provinceCode, districtCode, wardCode) {
    // Tải toàn bộ dữ liệu
    await loadAllData();
    if (provinceCode) {
        // Chọn tỉnh/thành phố
        citySelect.value = provinceCode;
        $(citySelect).selectpicker('refresh');
        // Kích hoạt sự kiện change để tải quận/huyện
        const changeEvent = new Event('change');
        citySelect.dispatchEvent(changeEvent);

        if (districtCode) {
            // Chọn quận/huyện
            districtSelect.value = districtCode;
            $(districtSelect).selectpicker('refresh');
            // Kích hoạt sự kiện change để tải phường/xã
            districtSelect.dispatchEvent(changeEvent);

            if (wardCode) {
                // Chọn phường/xã
                wardSelect.value = wardCode;
                $(wardSelect).selectpicker('refresh');
            }
        }
    }
}
