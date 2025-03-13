function extractMaterials() {
    var fileInput = $("#cadFile")[0];
    if (!fileInput.files[0]) {
        alert("Please select a file.");
        return;
    }

    var formData = new FormData();
    formData.append("file", fileInput.files[0]);

    $.ajax({
        url: "/api/materials/extract",
        type: "POST",
        data: formData,
        processData: false,
        contentType: false,
        success: function (data) {
            var tbody = $("#materialsTable tbody");
            tbody.empty();
            data.forEach(function (item) {
                tbody.append(`<tr><td>${item.name}</td><td>${item.quantity}</td></tr>`);
            });
        },
        error: function (xhr) {
            alert("Error: " + xhr.responseText);
        }
    });
}