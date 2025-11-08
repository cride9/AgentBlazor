// Manages theme by adding/removing 'dark' class from <html> and interacting with localStorage
window.themeInterop = {
    // This is called once when the app starts
    initializeTheme: () => {
        const theme = localStorage.getItem('theme') || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
        if (theme === 'dark') {
            document.documentElement.classList.add('dark');
        } else {
            document.documentElement.classList.remove('dark');
        }
        return theme; // Return the determined theme to Blazor
    },
    // This is called when the user clicks the toggle button
    setTheme: (theme) => {
        localStorage.setItem('theme', theme);
        if (theme === 'dark') {
            document.documentElement.classList.add('dark');
        } else {
            document.documentElement.classList.remove('dark');
        }
    }
};

window.llmMarkdown = {
    set: (element, text) => {
        if (!element) return;
        element.innerHTML = DOMPurify.sanitize(marked.parse(text || ""));
    }
};

// Auto-resizes a textarea based on its content
window.textAreaInterop = {
    autoResize: (element) => {
        if (element) {
            element.style.height = 'auto'; // Reset height
            element.style.height = (element.scrollHeight) + 'px';
        }
    },
    blurElement: function (element) {
        element?.blur();
    }
};

// Manages the browser resize listener
window.resizeInterop = {
    // The Blazor component will pass an object reference to itself
    registerResizeCallback: (blazorComponent) => {
        // Define the function that will be called on resize
        const resizeHandler = () => {
            // Call the [JSInvokable] C# method named 'OnBrowserResize'
            blazorComponent.invokeMethodAsync('OnBrowserResize');
        };

        // Add the event listener
        window.addEventListener('resize', resizeHandler);

        // Store the handler reference so we can remove it later
        window.resizeInterop.handler = resizeHandler;
    },
    // This is called when the Blazor component is disposed
    unregisterResizeCallback: () => {
        if (window.resizeInterop.handler) {
            window.removeEventListener('resize', window.resizeInterop.handler);
        }
    }
};

window.fileDropInterop = {
    initialize: (dropZoneElement, dotNetHelper) => {
        // Prevent default drag behaviors
        ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
            dropZoneElement.addEventListener(eventName, e => {
                e.preventDefault();
                e.stopPropagation();
            }, false);
        });

        // Set state on drag enter
        dropZoneElement.addEventListener('dragenter', () => {
            dotNetHelper.invokeMethodAsync('SetDragOver', true);
        });

        // Unset state on drag leave
        dropZoneElement.addEventListener('dragleave', e => {
            if (!dropZoneElement.contains(e.relatedTarget)) {
                dotNetHelper.invokeMethodAsync('SetDragOver', false);
            }
        });

        // Handle the file drop
        dropZoneElement.addEventListener('drop', async e => {
            dotNetHelper.invokeMethodAsync('SetDragOver', false);

            const files = e.dataTransfer.files;
            if (files.length > 0) {
                // We'll just handle the first file for simplicity
                const file = files[0];
                const reader = new FileReader();

                reader.onload = () => {
                    const arrayBuffer = reader.result;
                    const uint8Array = new Uint8Array(arrayBuffer);
                    dotNetHelper.invokeMethodAsync('HandleFileDrop', file.name, uint8Array);
                };

                reader.readAsArrayBuffer(file);
            }
        });
    }
};