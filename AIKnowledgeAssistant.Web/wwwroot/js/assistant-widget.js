(function () {
    // Locate the current script element to extract data attributes
    var currentScript = document.currentScript || (function () {
        var scripts = document.getElementsByTagName('script');
        return scripts[scripts.length - 1];
    })();

    var token = currentScript.getAttribute('data-token') || '';
    var endpoint = currentScript.getAttribute('data-endpoint') || window.location.origin + '/api/ask';
    var themeColor = currentScript.getAttribute('data-color') || '#4f46e5';

    if (!token) {
        console.error('[AI Assistant Widget] Missing required data-token attribute.');
        return;
    }

    var baseUrl = endpoint.replace('/api/ask', '');
    var iframeUrl = baseUrl + '/assistant/' + token + '?embed=true';

    // 1. Check if an inline container was designated
    var container = document.getElementById('ai-assistant-container');
    if (container) {
        var inlineIframe = document.createElement('iframe');
        inlineIframe.src = iframeUrl;
        inlineIframe.style.width = '100%';
        inlineIframe.style.height = '620px';
        inlineIframe.style.border = '1px solid #e2e8f0';
        inlineIframe.style.borderRadius = '12px';
        inlineIframe.style.boxShadow = '0 4px 6px -1px rgba(0,0,0,0.05)';
        inlineIframe.setAttribute('allow', 'clipboard-write');
        container.appendChild(inlineIframe);
        return;
    }

    // 2. Otherwise create a floating launcher bubble & modal window
    var widgetStyle = document.createElement('style');
    widgetStyle.innerHTML = `
        .ai-widget-launcher {
            position: fixed;
            bottom: 24px;
            right: 24px;
            width: 60px;
            height: 60px;
            border-radius: 30px;
            background-color: ${themeColor};
            color: #ffffff;
            box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.2);
            display: flex;
            align-items: center;
            justify-content: center;
            cursor: pointer;
            z-index: 999998;
            transition: transform 0.2s cubic-bezier(0.34, 1.56, 0.64, 1), box-shadow 0.2s ease;
        }
        .ai-widget-launcher:hover {
            transform: scale(1.08);
            box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.25);
        }
        .ai-widget-frame {
            position: fixed;
            bottom: 96px;
            right: 24px;
            width: 400px;
            height: 600px;
            max-width: calc(100vw - 32px);
            max-height: calc(100vh - 120px);
            border-radius: 16px;
            border: 1px solid #e2e8f0;
            background: #ffffff;
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
            z-index: 999999;
            overflow: hidden;
            display: none;
            flex-direction: column;
            animation: aiWidgetFadeIn 0.2s ease-out;
        }
        @keyframes aiWidgetFadeIn {
            from { opacity: 0; transform: translateY(12px); }
            to { opacity: 1; transform: translateY(0); }
        }
    `;
    document.head.appendChild(widgetStyle);

    // Launcher button
    var launcher = document.createElement('div');
    launcher.className = 'ai-widget-launcher';
    launcher.innerHTML = `
        <svg id="aiWidgetIconChat" width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"></path>
        </svg>
        <svg id="aiWidgetIconClose" style="display:none;" width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <line x1="18" y1="6" x2="6" y2="18"></line>
            <line x1="6" y1="6" x2="18" y2="18"></line>
        </svg>
    `;

    // Modal frame
    var frameContainer = document.createElement('div');
    frameContainer.className = 'ai-widget-frame';
    frameContainer.innerHTML = `<iframe src="${iframeUrl}" style="width:100%; height:100%; border:none;" allow="clipboard-write"></iframe>`;

    document.body.appendChild(launcher);
    document.body.appendChild(frameContainer);

    var isOpen = false;
    launcher.addEventListener('click', function () {
        isOpen = !isOpen;
        if (isOpen) {
            frameContainer.style.display = 'flex';
            document.getElementById('aiWidgetIconChat').style.display = 'none';
            document.getElementById('aiWidgetIconClose').style.display = 'block';
        } else {
            frameContainer.style.display = 'none';
            document.getElementById('aiWidgetIconChat').style.display = 'block';
            document.getElementById('aiWidgetIconClose').style.display = 'none';
        }
    });
})();
