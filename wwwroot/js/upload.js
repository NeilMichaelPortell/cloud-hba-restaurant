document.getElementById('uploadBtn').addEventListener('click', function () {
    const files = document.getElementById('fileInput').files;
    const restaurantName = document.getElementById('restaurantName').value.trim();
    const container = document.getElementById('progressContainer');

    if (!restaurantName) {
        alert('Please enter a restaurant name.');
        return;
    }

    if (files.length === 0) {
        alert('Please select at least one image.');
        return;
    }

    container.innerHTML = '';

    Array.from(files).forEach(function (file) {
        // Unique key safe for DOM ids
        const safeKey = file.name.replace(/[^a-zA-Z0-9]/g, '_') + '_' + Date.now();

        const card = document.createElement('div');
        card.className = 'card mb-2 p-3';
        card.innerHTML = `
            <div class="d-flex justify-content-between align-items-center mb-1">
                <span class="fw-semibold">${file.name}</span>
                <span class="badge bg-secondary" id="status-${safeKey}">Waiting</span>
            </div>
            <div class="progress" style="height: 22px;">
                <div id="bar-${safeKey}"
                     class="progress-bar progress-bar-striped progress-bar-animated"
                     role="progressbar"
                     style="width: 0%">0%</div>
            </div>
            <small class="text-muted mt-1 d-block" id="msg-${safeKey}"></small>
        `;
        container.appendChild(card);

        uploadFile(file, restaurantName, safeKey);
    });
});

function uploadFile(file, restaurantName, safeKey) {
    const bar    = document.getElementById('bar-'    + safeKey);
    const status = document.getElementById('status-' + safeKey);
    const msg    = document.getElementById('msg-'    + safeKey);

    const formData = new FormData();
    formData.append('file', file);
    formData.append('restaurantName', restaurantName);

    const xhr = new XMLHttpRequest();

    // Real-time progress synchronized with actual upload bytes
    xhr.upload.addEventListener('progress', function (e) {
        if (e.lengthComputable) {
            const percent = Math.round((e.loaded / e.total) * 100);
            bar.style.width = percent + '%';
            bar.textContent = percent + '%';
            status.className = 'badge bg-primary';
            status.textContent = 'Uploading';
        }
    });

    // Upload complete
    xhr.addEventListener('load', function () {
        if (xhr.status === 200) {
            bar.style.width = '100%';
            bar.textContent = '100%';
            bar.classList.remove('progress-bar-animated', 'bg-primary');
            bar.classList.add('bg-success');
            status.className = 'badge bg-success';
            status.textContent = 'Done';
            msg.textContent = 'Uploaded successfully. Processing will begin shortly.';
        } else {
            let errorMsg = 'Server error: ' + xhr.status;
            try {
                const resp = JSON.parse(xhr.responseText);
                if (resp.message) errorMsg = resp.message;
            } catch (e) {}
            markFailed(bar, status, msg, errorMsg);
        }
    });

    // Network failure
    xhr.addEventListener('error', function () {
        markFailed(bar, status, msg, 'Network error — upload failed.');
    });

    xhr.open('POST', '/Menu/UploadImage');
    xhr.send(formData);
}

function markFailed(bar, status, msg, errorText) {
    bar.classList.remove('progress-bar-animated', 'bg-primary');
    bar.classList.add('bg-danger');
    bar.textContent = 'Failed';
    status.className = 'badge bg-danger';
    status.textContent = 'Error';
    msg.textContent = errorText;
}
