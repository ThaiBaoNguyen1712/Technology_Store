const fetchData = async (url) => {
    const response = await fetch(url);
    return response.json();
};

const populateSelect = (selectElement, data, textKey = 'name', valueKey = 'code', idKey = 'id') => {
    selectElement.innerHTML = '';
    data.forEach(item => {
        const option = document.createElement('option');
        option.value = item[valueKey];
        option.textContent = item[textKey];
        option.dataset.id = item[idKey];
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
let cities = [];
let districts = [];
let wards = [];

// Tải danh sách tỉnh/thành phố
const loadCities = async () => {
    cities = await fetchData('https://provinces.open-api.vn/api/p/');
    populateSelect(citySelect, cities);
};

// Xử lý sự kiện khi chọn tỉnh/thành phố
citySelect.addEventListener('change', async function () {
    const selectedCityCode = this.value;
    if (selectedCityCode) {
        const cityData = await fetchData(`https://provinces.open-api.vn/api/p/${selectedCityCode}?depth=2`);
        districts = cityData.districts;
        populateSelect(districtSelect, districts);
        $(wardSelect).empty().selectpicker('refresh');
    } else {
        $(districtSelect).empty().selectpicker('refresh');
        $(wardSelect).empty().selectpicker('refresh');
    }
});

// Xử lý sự kiện khi chọn quận/huyện
districtSelect.addEventListener('change', async function () {
    const selectedDistrictCode = this.value;
    if (selectedDistrictCode) {
        console.log(selectedDistrictCode);
        const districtData = await fetchData(`https://provinces.open-api.vn/api/d/${selectedDistrictCode}?depth=2`);
        wards = districtData.wards;
        populateSelect(wardSelect, wards);
    } else {
        $(wardSelect).empty().selectpicker('refresh');
    }
});

// Xử lý sự kiện khi chọn phường/xã
wardSelect.addEventListener('change', function () {
    const selectedWardId = this.options[this.selectedIndex].dataset.id;
    console.log('Selected Ward ID:', selectedWardId);
});

// Khởi tạo khi trang được tải
document.addEventListener('DOMContentLoaded', function () {
    $('.selectpicker').selectpicker({
        liveSearch: true,
        size: 10
    });
    loadCities();
});

async function fillAddressData(provinceCode, districtCode, wardCode) {
    // Tải dữ liệu tỉnh/thành phố
    await loadCities();

    if (provinceCode) {
        // Chọn tỉnh/thành phố
        citySelect.value = provinceCode;
        $(citySelect).selectpicker('refresh');

        // Kích hoạt sự kiện change để tải quận/huyện
        const changeEvent = new Event('change');
        citySelect.dispatchEvent(changeEvent);

        // Đợi dữ liệu quận/huyện được tải
        await new Promise(resolve => setTimeout(resolve, 500));

        if (districtCode) {
            // Chọn quận/huyện
            districtSelect.value = districtCode;
            $(districtSelect).selectpicker('refresh');

            // Kích hoạt sự kiện change để tải phường/xã
            districtSelect.dispatchEvent(changeEvent);

            // Đợi dữ liệu phường/xã được tải
            await new Promise(resolve => setTimeout(resolve, 500));

            if (wardCode) {
                // Chọn phường/xã
                wardSelect.value = wardCode;
                $(wardSelect).selectpicker('refresh');
            }
        }
    }
}
