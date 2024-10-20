// Wait for the DOM to be fully loaded
document.addEventListener('DOMContentLoaded', function () {
    // Initialize Bootstrap components
    initializeBootstrapComponents();

    // Setup Dropzone for file uploads
    setupDropzone('thumbnailDropzone', 'Image', 'thumbnailPreview', true);
    setupDropzone('additionalDropzone', 'Gallery', 'additionalPreview');

    // Initialize CKEditor
    if (typeof CKEDITOR !== 'undefined') {
        CKEDITOR.replace('description');
    }

    // Setup event listeners
    setupEventListeners();

    // Initial UI updates
    updateDiscountFields();
    updateVariantTable();
});

function initializeBootstrapComponents() {
    $('.selectpicker').selectpicker();

    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    $('[data-toggle="tooltip"]').tooltip();
}

function setupEventListeners() {
    $('#DiscountType').on('change', updateDiscountFields);
    $('#OriginalPrice, #discountAmount, #discountPercentage').on('input', calculateSellPrice);
    $('#divColor, #romSelect, #materialInput, #sizeSelect').on('change', updateVariantTable);
    $('#variantSelect').on('changed.bs.select', handleVariantSelectChange);
    $('#videoUrl').on('change', handleVideoUrlChange);
    $('#ProductForm').on('submit', handleFormSubmit);
    $(document).on('click', '.delete-btn', handleVariantDelete);
}

function generateSku() {
    const randomNum = Math.floor(10000 + Math.random() * 90000);
    const sku = `${randomNum}`;
    $('#SkuCode').val(sku);
}

function updateVariantTable() {
    var colors = $('#divColor').val() || [];
    var rom = $('#romSelect').val() || [];
    var material = $('#materialInput').val().charAt(0).toUpperCase() || '';
    var sizes = $('#sizeSelect').val() || [];

    if (colors.length === 0) return;

    var variants = generateVariants(colors, rom, sizes, material);
    renderVariantTable(variants);
}

function generateVariants(colors, rom, sizes, material) {
    var variants = [];
    colors.forEach(function (color) {
        var baseVariant = { color: color };
        if (rom.length > 0) {
            rom.forEach(function (r) {
                var romVariant = { ...baseVariant, rom: r };
                if (sizes.length > 0) {
                    sizes.forEach(function (size) {
                        variants.push({ ...romVariant, size: size, material: material });
                    });
                } else {
                    variants.push({ ...romVariant, material: material });
                }
            });
        } else if (sizes.length > 0) {
            sizes.forEach(function (size) {
                variants.push({ ...baseVariant, size: size, material: material });
            });
        } else {
            variants.push({ ...baseVariant, material: material });
        }
    });
    return variants;
}

function renderVariantTable(variants) {
    var tableBody = $('#variantTableBody');
    tableBody.empty();

    variants.forEach(function (variant, index) {
        var variantString = Object.entries(variant)
            .filter(([key, value]) => value !== '')
            .map(([key, value]) => `${key}: ${value}`)
            .join(', ');
        var skuProduct = $('#SkuCode').val() || '';
        var sku = skuProduct + '-' + Object.values(variant).filter(v => v !== '').join('-');
        var price = $('#sellPrice').val();

        var row = $('<tr>');
        row.append($('<td>').text(index + 1));
        row.append($('<td style="display:none">').html('<input type="hidden" name="VarientProducts[' + index + '].Attributes" value="' + variantString + '"/>'));
        row.append($('<td>').text(variantString));
        row.append($('<td>').html('<input type="number" class="form-control" name="VarientProducts[' + index + '].Price" value="' + price + '"/>'));
        row.append($('<td>').html('<input type="text" class="form-control" name="VarientProducts[' + index + '].Sku" value="' + sku + '"/>'));
        row.append($('<td>').html('<input type="number" class="form-control" name="VarientProducts[' + index + '].Stock" value="1" required/>'));
        row.append($('<td style="width:10%">').html('<i class="fas fa-trash delete-btn text-center text-danger" title="Xóa"></i>'));

        tableBody.append(row);
    });

    $('.table-Varient').show();
    updateRowNumbers();
}

function handleVariantSelectChange(e, clickedIndex, isSelected, previousValue) {
    var selectedOptions = $(this).val() || [];
    $('#divrom, #divMaterial, #divSize').hide();
    if (selectedOptions.includes('rom')) $('#divrom').show();
    if (selectedOptions.includes('material')) $('#divMaterial').show();
    if (selectedOptions.includes('size')) $('#divSize').show();
    updateVariantTable();
}

function updateRowNumbers() {
    $('#variantTableBody tr').each(function (index) {
        $(this).find('td:first').text(index + 1);
        $(this).find('input').each(function () {
            var name = $(this).attr('name');
            if (name) {
                var newName = name.replace(/\[\d+\]/, '[' + index + ']');
                $(this).attr('name', newName);
            }
        });
    });
}

function handleVariantDelete() {
    $(this).closest('tr').remove();
    updateRowNumbers();
}

function handleVideoUrlChange() {
    const url = $(this).val();
    const videoId = extractVideoID(url);
    if (videoId) {
        $('#videoPreview').html(`<iframe width="560" height="315" src="https://www.youtube.com/embed/${videoId}" frameborder="0" allowfullscreen></iframe>`);
    } else {
        $('#videoPreview').empty();
    }
}

function extractVideoID(url) {
    const regExp = /^.*((youtu.be\/)|(v\/)|(\/u\/\w\/)|(embed\/)|(watch\?))\??v?=?([^#&?]*).*/;
    const match = url.match(regExp);
    return (match && match[7].length == 11) ? match[7] : false;
}

function setupDropzone(dropzoneId, inputName, previewId, isSingle = false) {
    const dropzone = document.getElementById(dropzoneId);
    const input = dropzone.querySelector('input[type="file"]');
    const preview = document.getElementById(previewId);

    dropzone.addEventListener('click', () => input.click());
    dropzone.addEventListener('dragover', (e) => {
        e.preventDefault();
        dropzone.classList.add('dragover');
    });
    dropzone.addEventListener('dragleave', () => {
        dropzone.classList.remove('dragover');
    });
    dropzone.addEventListener('drop', (e) => {
        e.preventDefault();
        dropzone.classList.remove('dragover');
        handleFiles(e.dataTransfer.files);
    });

    input.addEventListener('change', () => handleFiles(input.files));

    function handleFiles(files) {
        if (isSingle) {
            preview.innerHTML = '';
            files = [files[0]];
        }
        Array.from(files).forEach(file => {
            if (file.type.startsWith('image/') && file.size <= 2 * 1024 * 1024) {
                const reader = new FileReader();
                reader.onload = (e) => {
                    const div = document.createElement('div');
                    div.className = 'preview-image';
                    div.innerHTML = `
                        <img src="${e.target.result}" alt="Preview">
                        <button class="remove-btn">&times;</button>
                    `;
                    div.querySelector('.remove-btn').addEventListener('click', () => {
                        div.remove();
                        updateInputFiles();
                    });
                    preview.appendChild(div);
                    updateInputFiles();
                };
                reader.readAsDataURL(file);
            } else {
                alert(`File ${file.name} không hợp lệ. Chỉ chấp nhận hình ảnh dưới 2MB.`);
            }
        });
    }

    function updateInputFiles() {
        const dataTransfer = new DataTransfer();
        preview.querySelectorAll('img').forEach(img => {
            fetch(img.src)
                .then(res => res.blob())
                .then(blob => {
                    const file = new File([blob], "image.jpg", { type: "image/jpeg" });
                    dataTransfer.items.add(file);
                    input.files = dataTransfer.files;
                });
        });
    }
}

function updateDiscountFields() {
    var selectedValue = $('#DiscountType').val();
    $('#cash, #percent').hide();
    if (selectedValue === "1") {
        $('#cash').show();
    } else if (selectedValue === "2") {
        $('#percent').show();
    }
}

function calculateSellPrice() {
    let originalPrice = parseFloat($('#OriginalPrice').val()) || 0;
    let discountType = $('#DiscountType').val();
    let sellPrice = 0;

    if (discountType == '1') {
        let discountAmount = parseFloat($('#discountAmount').val()) || 0;
        sellPrice = originalPrice - discountAmount;
    } else if (discountType == '2') {
        let discountPercentage = parseFloat($('#discountPercentage').val()) || 0;
        sellPrice = originalPrice - (originalPrice * discountPercentage / 100);
    }

    sellPrice = Math.max(sellPrice, 0);
    $('#sellPrice').val(sellPrice.toFixed(2));
}

function handleFormSubmit(e) {
    e.preventDefault();
    $('.loading-container').show();

    var formData = new FormData(this);
    $.ajax({
        method: 'POST',
        url: '/Products/Create',
        data: formData,
        processData: false,
        contentType: false,
        headers: {
            "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (res) {
            $('.loading-container').hide();
            alert('Thành công!');
        },
        error: function (xhr, error, status) {
            $('.loading-container').hide();
            alert('Đã xảy ra lỗi: ' + xhr.responseText);
            console.error('Error details:', error);
        }
    });
}// Wait for the DOM to be fully loaded
document.addEventListener('DOMContentLoaded', function () {
    // Initialize Bootstrap components
    initializeBootstrapComponents();

    // Setup Dropzone for file uploads
    setupDropzone('thumbnailDropzone', 'Image', 'thumbnailPreview', true);
    setupDropzone('additionalDropzone', 'Gallery', 'additionalPreview');

    // Initialize CKEditor
    if (typeof CKEDITOR !== 'undefined') {
        CKEDITOR.replace('description');
    }

    // Setup event listeners
    setupEventListeners();

    // Initial UI updates
    updateDiscountFields();
    updateVariantTable();
});

function initializeBootstrapComponents() {
    $('.selectpicker').selectpicker();

    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    $('[data-toggle="tooltip"]').tooltip();
}

function setupEventListeners() {
    $('#DiscountType').on('change', updateDiscountFields);
    $('#OriginalPrice, #discountAmount, #discountPercentage').on('input', calculateSellPrice);
    $('#divColor, #romSelect, #materialInput, #sizeSelect').on('change', updateVariantTable);
    $('#variantSelect').on('changed.bs.select', handleVariantSelectChange);
    $('#videoUrl').on('change', handleVideoUrlChange);
    $('#ProductForm').on('submit', handleFormSubmit);
    $(document).on('click', '.delete-btn', handleVariantDelete);
}

function generateSku() {
    const randomNum = Math.floor(10000 + Math.random() * 90000);
    const sku = `${randomNum}`;
    $('#SkuCode').val(sku);
}

function updateVariantTable() {
    var colors = $('#divColor').val() || [];
    var rom = $('#romSelect').val() || [];
    var material = $('#materialInput').val().charAt(0).toUpperCase() || '';
    var sizes = $('#sizeSelect').val() || [];

    if (colors.length === 0) return;

    var variants = generateVariants(colors, rom, sizes, material);
    renderVariantTable(variants);
}

function generateVariants(colors, rom, sizes, material) {
    var variants = [];
    colors.forEach(function (color) {
        var baseVariant = { color: color };
        if (rom.length > 0) {
            rom.forEach(function (r) {
                var romVariant = { ...baseVariant, rom: r };
                if (sizes.length > 0) {
                    sizes.forEach(function (size) {
                        variants.push({ ...romVariant, size: size, material: material });
                    });
                } else {
                    variants.push({ ...romVariant, material: material });
                }
            });
        } else if (sizes.length > 0) {
            sizes.forEach(function (size) {
                variants.push({ ...baseVariant, size: size, material: material });
            });
        } else {
            variants.push({ ...baseVariant, material: material });
        }
    });
    return variants;
}

function renderVariantTable(variants) {
    var tableBody = $('#variantTableBody');
    tableBody.empty();

    variants.forEach(function (variant, index) {
        var variantString = Object.entries(variant)
            .filter(([key, value]) => value !== '')
            .map(([key, value]) => `${key}: ${value}`)
            .join(', ');
        var skuProduct = $('#SkuCode').val() || '';
        var sku = skuProduct + '-' + Object.values(variant).filter(v => v !== '').join('-');
        var price = $('#sellPrice').val();

        var row = $('<tr>');
        row.append($('<td>').text(index + 1));
        row.append($('<td style="display:none">').html('<input type="hidden" name="VarientProducts[' + index + '].Attributes" value="' + variantString + '"/>'));
        row.append($('<td>').text(variantString));
        row.append($('<td>').html('<input type="number" class="form-control" name="VarientProducts[' + index + '].Price" value="' + price + '"/>'));
        row.append($('<td>').html('<input type="text" class="form-control" name="VarientProducts[' + index + '].Sku" value="' + sku + '"/>'));
        row.append($('<td>').html('<input type="number" class="form-control" name="VarientProducts[' + index + '].Stock" value="1" required/>'));
        row.append($('<td style="width:10%">').html('<i class="fas fa-trash delete-btn text-center text-danger" title="Xóa"></i>'));

        tableBody.append(row);
    });

    $('.table-Varient').show();
    updateRowNumbers();
}

function handleVariantSelectChange(e, clickedIndex, isSelected, previousValue) {
    var selectedOptions = $(this).val() || [];
    $('#divrom, #divMaterial, #divSize').hide();
    if (selectedOptions.includes('rom')) $('#divrom').show();
    if (selectedOptions.includes('material')) $('#divMaterial').show();
    if (selectedOptions.includes('size')) $('#divSize').show();
    updateVariantTable();
}

function updateRowNumbers() {
    $('#variantTableBody tr').each(function (index) {
        $(this).find('td:first').text(index + 1);
        $(this).find('input').each(function () {
            var name = $(this).attr('name');
            if (name) {
                var newName = name.replace(/\[\d+\]/, '[' + index + ']');
                $(this).attr('name', newName);
            }
        });
    });
}

function handleVariantDelete() {
    $(this).closest('tr').remove();
    updateRowNumbers();
}

function handleVideoUrlChange() {
    const url = $(this).val();
    const videoId = extractVideoID(url);
    if (videoId) {
        $('#videoPreview').html(`<iframe width="560" height="315" src="https://www.youtube.com/embed/${videoId}" frameborder="0" allowfullscreen></iframe>`);
    } else {
        $('#videoPreview').empty();
    }
}

function extractVideoID(url) {
    const regExp = /^.*((youtu.be\/)|(v\/)|(\/u\/\w\/)|(embed\/)|(watch\?))\??v?=?([^#&?]*).*/;
    const match = url.match(regExp);
    return (match && match[7].length == 11) ? match[7] : false;
}

function setupDropzone(dropzoneId, inputName, previewId, isSingle = false) {
    const dropzone = document.getElementById(dropzoneId);
    const input = dropzone.querySelector('input[type="file"]');
    const preview = document.getElementById(previewId);

    dropzone.addEventListener('click', () => input.click());
    dropzone.addEventListener('dragover', (e) => {
        e.preventDefault();
        dropzone.classList.add('dragover');
    });
    dropzone.addEventListener('dragleave', () => {
        dropzone.classList.remove('dragover');
    });
    dropzone.addEventListener('drop', (e) => {
        e.preventDefault();
        dropzone.classList.remove('dragover');
        handleFiles(e.dataTransfer.files);
    });

    input.addEventListener('change', () => handleFiles(input.files));

    function handleFiles(files) {
        if (isSingle) {
            preview.innerHTML = '';
            files = [files[0]];
        }
        Array.from(files).forEach(file => {
            if (file.type.startsWith('image/') && file.size <= 2 * 1024 * 1024) {
                const reader = new FileReader();
                reader.onload = (e) => {
                    const div = document.createElement('div');
                    div.className = 'preview-image';
                    div.innerHTML = `
                        <img src="${e.target.result}" alt="Preview">
                        <button class="remove-btn">&times;</button>
                    `;
                    div.querySelector('.remove-btn').addEventListener('click', () => {
                        div.remove();
                        updateInputFiles();
                    });
                    preview.appendChild(div);
                    updateInputFiles();
                };
                reader.readAsDataURL(file);
            } else {
                alert(`File ${file.name} không hợp lệ. Chỉ chấp nhận hình ảnh dưới 2MB.`);
            }
        });
    }

    function updateInputFiles() {
        const dataTransfer = new DataTransfer();
        preview.querySelectorAll('img').forEach(img => {
            fetch(img.src)
                .then(res => res.blob())
                .then(blob => {
                    const file = new File([blob], "image.jpg", { type: "image/jpeg" });
                    dataTransfer.items.add(file);
                    input.files = dataTransfer.files;
                });
        });
    }
}

function updateDiscountFields() {
    var selectedValue = $('#DiscountType').val();
    $('#cash, #percent').hide();
    if (selectedValue === "1") {
        $('#cash').show();
    } else if (selectedValue === "2") {
        $('#percent').show();
    }
}

function calculateSellPrice() {
    let originalPrice = parseFloat($('#OriginalPrice').val()) || 0;
    let discountType = $('#DiscountType').val();
    let sellPrice = 0;

    if (discountType == '1') {
        let discountAmount = parseFloat($('#discountAmount').val()) || 0;
        sellPrice = originalPrice - discountAmount;
    } else if (discountType == '2') {
        let discountPercentage = parseFloat($('#discountPercentage').val()) || 0;
        sellPrice = originalPrice - (originalPrice * discountPercentage / 100);
    }

    sellPrice = Math.max(sellPrice, 0);
    $('#sellPrice').val(sellPrice.toFixed(2));
}

function handleFormSubmit(e) {
    e.preventDefault();
    $('.loading-container').show();

    var formData = new FormData(this);
    $.ajax({
        method: 'POST',
        url: '/Products/Create',
        data: formData,
        processData: false,
        contentType: false,
        headers: {
            "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (res) {
            $('.loading-container').hide();
            alert('Thành công!');
        },
        error: function (xhr, error, status) {
            $('.loading-container').hide();
            alert('Đã xảy ra lỗi: ' + xhr.responseText);
            console.error('Error details:', error);
        }
    });
}