
/*FilePond*/
FilePond.registerPlugin(
    FilePondPluginImagePreview,
    FilePondPluginImageExifOrientation
);

// Áp dụng FilePond cho tất cả các input có class filepond
document.querySelectorAll('input.filepond').forEach(input => {
    FilePond.create(input);
});
