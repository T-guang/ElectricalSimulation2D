mergeInto(LibraryManager.library, {
    ImportFileWebGL: function () {
        var fileInput = document.getElementById('webgl-file-input');
        if (!fileInput) {
            fileInput = document.createElement('input');
            fileInput.setAttribute('type', 'file');
            fileInput.setAttribute('id', 'webgl-file-input');
            fileInput.setAttribute('accept', '.json,application/json');
            fileInput.style.display = 'none';
            document.body.appendChild(fileInput);
        }

        fileInput.onchange = function (event) {
            var file = event.target.files[0];
            if (!file) {
                SendMessage('NativeFileBrowserReceiver', 'OnWebGLFileError', '未选择文件');
                return;
            }
            if (!file.name.endsWith('.json')) {
                SendMessage('NativeFileBrowserReceiver', 'OnWebGLFileError', '请选择由本系统导出的 JSON 图纸文件。');
                return;
            }
            if (file.size > 5 * 1024 * 1024) {
                SendMessage('NativeFileBrowserReceiver', 'OnWebGLFileError', '文件过大，请确保图纸不超过 5MB');
                return;
            }

            var reader = new FileReader();
            reader.onload = function (e) {
                SendMessage('NativeFileBrowserReceiver', 'OnWebGLFileLoaded', e.target.result);
            };
            reader.onerror = function () {
                SendMessage('NativeFileBrowserReceiver', 'OnWebGLFileError', '读取文件失败');
            };
            reader.readAsText(file, "utf-8");

            fileInput.value = '';
        };

        fileInput.click();
    }
});
