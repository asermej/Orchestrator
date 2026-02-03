/**
 * Hireology AI Chatbot Widget
 * 
 * Embed this widget on any career site to enable AI-powered conversations
 * with candidates about jobs and company information.
 * 
 * Usage:
 * <script src="https://your-domain.com/widget.js" data-agent-id="YOUR_AGENT_ID"></script>
 */

(function() {
  'use strict';

  // Configuration
  const CONFIG = {
    apiBaseUrl: window.HireologyWidget?.apiBaseUrl || 'http://localhost:5000',
    agentId: null,
    primaryColor: window.HireologyWidget?.primaryColor || '#0f766e',
    position: window.HireologyWidget?.position || 'bottom-right',
    greeting: window.HireologyWidget?.greeting || 'Hi! I\'m here to help answer your questions about this opportunity.',
    placeholder: window.HireologyWidget?.placeholder || 'Type your question...',
    title: window.HireologyWidget?.title || 'AI Assistant'
  };

  // Get agent ID from script tag
  const currentScript = document.currentScript || document.querySelector('script[data-agent-id]');
  if (currentScript) {
    CONFIG.agentId = currentScript.getAttribute('data-agent-id');
    
    // Override config from data attributes
    if (currentScript.dataset.apiBaseUrl) CONFIG.apiBaseUrl = currentScript.dataset.apiBaseUrl;
    if (currentScript.dataset.primaryColor) CONFIG.primaryColor = currentScript.dataset.primaryColor;
    if (currentScript.dataset.position) CONFIG.position = currentScript.dataset.position;
    if (currentScript.dataset.greeting) CONFIG.greeting = currentScript.dataset.greeting;
    if (currentScript.dataset.title) CONFIG.title = currentScript.dataset.title;
  }

  // Widget State
  let isOpen = false;
  let isLoading = false;
  let messages = [];
  let sessionId = null;

  // Create widget styles
  function injectStyles() {
    const styles = document.createElement('style');
    styles.textContent = `
      .hireology-widget-container {
        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
        position: fixed;
        z-index: 999999;
        ${CONFIG.position === 'bottom-right' ? 'right: 20px; bottom: 20px;' : 'left: 20px; bottom: 20px;'}
      }

      .hireology-widget-button {
        width: 60px;
        height: 60px;
        border-radius: 50%;
        background: ${CONFIG.primaryColor};
        border: none;
        cursor: pointer;
        box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        display: flex;
        align-items: center;
        justify-content: center;
        transition: transform 0.2s, box-shadow 0.2s;
      }

      .hireology-widget-button:hover {
        transform: scale(1.05);
        box-shadow: 0 6px 16px rgba(0,0,0,0.2);
      }

      .hireology-widget-button svg {
        width: 28px;
        height: 28px;
        fill: white;
      }

      .hireology-widget-panel {
        position: absolute;
        ${CONFIG.position === 'bottom-right' ? 'right: 0;' : 'left: 0;'}
        bottom: 70px;
        width: 380px;
        height: 550px;
        background: white;
        border-radius: 16px;
        box-shadow: 0 8px 32px rgba(0,0,0,0.15);
        display: none;
        flex-direction: column;
        overflow: hidden;
      }

      .hireology-widget-panel.open {
        display: flex;
        animation: hireology-slide-up 0.3s ease-out;
      }

      @keyframes hireology-slide-up {
        from {
          opacity: 0;
          transform: translateY(20px);
        }
        to {
          opacity: 1;
          transform: translateY(0);
        }
      }

      .hireology-widget-header {
        background: ${CONFIG.primaryColor};
        color: white;
        padding: 16px;
        display: flex;
        align-items: center;
        gap: 12px;
      }

      .hireology-widget-avatar {
        width: 40px;
        height: 40px;
        border-radius: 50%;
        background: rgba(255,255,255,0.2);
        display: flex;
        align-items: center;
        justify-content: center;
      }

      .hireology-widget-avatar svg {
        width: 24px;
        height: 24px;
        fill: white;
      }

      .hireology-widget-title {
        flex: 1;
      }

      .hireology-widget-title h3 {
        margin: 0;
        font-size: 16px;
        font-weight: 600;
      }

      .hireology-widget-title p {
        margin: 2px 0 0 0;
        font-size: 12px;
        opacity: 0.9;
      }

      .hireology-widget-close {
        background: none;
        border: none;
        cursor: pointer;
        padding: 4px;
        opacity: 0.8;
        transition: opacity 0.2s;
      }

      .hireology-widget-close:hover {
        opacity: 1;
      }

      .hireology-widget-close svg {
        width: 20px;
        height: 20px;
        fill: white;
      }

      .hireology-widget-messages {
        flex: 1;
        overflow-y: auto;
        padding: 16px;
        display: flex;
        flex-direction: column;
        gap: 12px;
      }

      .hireology-widget-message {
        max-width: 85%;
        padding: 12px 16px;
        border-radius: 16px;
        font-size: 14px;
        line-height: 1.5;
      }

      .hireology-widget-message.assistant {
        background: #f3f4f6;
        color: #1f2937;
        align-self: flex-start;
        border-bottom-left-radius: 4px;
      }

      .hireology-widget-message.user {
        background: ${CONFIG.primaryColor};
        color: white;
        align-self: flex-end;
        border-bottom-right-radius: 4px;
      }

      .hireology-widget-typing {
        display: flex;
        gap: 4px;
        padding: 12px 16px;
        background: #f3f4f6;
        border-radius: 16px;
        align-self: flex-start;
        border-bottom-left-radius: 4px;
      }

      .hireology-widget-typing span {
        width: 8px;
        height: 8px;
        background: #9ca3af;
        border-radius: 50%;
        animation: hireology-bounce 1.4s infinite ease-in-out both;
      }

      .hireology-widget-typing span:nth-child(1) { animation-delay: -0.32s; }
      .hireology-widget-typing span:nth-child(2) { animation-delay: -0.16s; }
      .hireology-widget-typing span:nth-child(3) { animation-delay: 0; }

      @keyframes hireology-bounce {
        0%, 80%, 100% { transform: scale(0); }
        40% { transform: scale(1); }
      }

      .hireology-widget-input-container {
        padding: 12px 16px;
        border-top: 1px solid #e5e7eb;
        display: flex;
        gap: 8px;
      }

      .hireology-widget-input {
        flex: 1;
        padding: 12px 16px;
        border: 1px solid #e5e7eb;
        border-radius: 24px;
        font-size: 14px;
        outline: none;
        transition: border-color 0.2s;
      }

      .hireology-widget-input:focus {
        border-color: ${CONFIG.primaryColor};
      }

      .hireology-widget-send {
        width: 44px;
        height: 44px;
        border-radius: 50%;
        background: ${CONFIG.primaryColor};
        border: none;
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center;
        transition: background 0.2s, transform 0.2s;
      }

      .hireology-widget-send:hover:not(:disabled) {
        background: ${adjustColor(CONFIG.primaryColor, -10)};
        transform: scale(1.05);
      }

      .hireology-widget-send:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }

      .hireology-widget-send svg {
        width: 20px;
        height: 20px;
        fill: white;
      }

      .hireology-widget-powered {
        text-align: center;
        padding: 8px;
        font-size: 11px;
        color: #9ca3af;
      }

      .hireology-widget-powered a {
        color: #6b7280;
        text-decoration: none;
      }

      .hireology-widget-powered a:hover {
        text-decoration: underline;
      }
    `;
    document.head.appendChild(styles);
  }

  // Helper function to adjust color brightness
  function adjustColor(color, percent) {
    const num = parseInt(color.replace('#', ''), 16);
    const amt = Math.round(2.55 * percent);
    const R = (num >> 16) + amt;
    const G = (num >> 8 & 0x00FF) + amt;
    const B = (num & 0x0000FF) + amt;
    return '#' + (0x1000000 + (R < 255 ? R < 1 ? 0 : R : 255) * 0x10000 +
      (G < 255 ? G < 1 ? 0 : G : 255) * 0x100 +
      (B < 255 ? B < 1 ? 0 : B : 255)).toString(16).slice(1);
  }

  // Create widget HTML
  function createWidget() {
    const container = document.createElement('div');
    container.className = 'hireology-widget-container';
    container.innerHTML = `
      <div class="hireology-widget-panel">
        <div class="hireology-widget-header">
          <div class="hireology-widget-avatar">
            <svg viewBox="0 0 24 24"><path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 3c1.66 0 3 1.34 3 3s-1.34 3-3 3-3-1.34-3-3 1.34-3 3-3zm0 14.2c-2.5 0-4.71-1.28-6-3.22.03-1.99 4-3.08 6-3.08 1.99 0 5.97 1.09 6 3.08-1.29 1.94-3.5 3.22-6 3.22z"/></svg>
          </div>
          <div class="hireology-widget-title">
            <h3>${CONFIG.title}</h3>
            <p>Ask me anything</p>
          </div>
          <button class="hireology-widget-close" aria-label="Close chat">
            <svg viewBox="0 0 24 24"><path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/></svg>
          </button>
        </div>
        <div class="hireology-widget-messages"></div>
        <div class="hireology-widget-input-container">
          <input type="text" class="hireology-widget-input" placeholder="${CONFIG.placeholder}">
          <button class="hireology-widget-send" aria-label="Send message">
            <svg viewBox="0 0 24 24"><path d="M2.01 21L23 12 2.01 3 2 10l15 2-15 2z"/></svg>
          </button>
        </div>
        <div class="hireology-widget-powered">
          Powered by <a href="https://hireology.com" target="_blank" rel="noopener">Hireology AI</a>
        </div>
      </div>
      <button class="hireology-widget-button" aria-label="Open chat">
        <svg viewBox="0 0 24 24"><path d="M20 2H4c-1.1 0-2 .9-2 2v18l4-4h14c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm0 14H6l-2 2V4h16v12z"/></svg>
      </button>
    `;
    document.body.appendChild(container);
    return container;
  }

  // Initialize widget
  function init() {
    if (!CONFIG.agentId) {
      console.warn('Hireology Widget: No agent ID provided. Add data-agent-id attribute to the script tag.');
      return;
    }

    injectStyles();
    const container = createWidget();
    
    const panel = container.querySelector('.hireology-widget-panel');
    const button = container.querySelector('.hireology-widget-button');
    const closeBtn = container.querySelector('.hireology-widget-close');
    const messagesContainer = container.querySelector('.hireology-widget-messages');
    const input = container.querySelector('.hireology-widget-input');
    const sendBtn = container.querySelector('.hireology-widget-send');

    // Generate session ID
    sessionId = 'session_' + Math.random().toString(36).substr(2, 9) + Date.now().toString(36);

    // Toggle panel
    function togglePanel() {
      isOpen = !isOpen;
      panel.classList.toggle('open', isOpen);
      if (isOpen && messages.length === 0) {
        // Add greeting message
        addMessage('assistant', CONFIG.greeting);
      }
      if (isOpen) {
        input.focus();
      }
    }

    button.addEventListener('click', togglePanel);
    closeBtn.addEventListener('click', togglePanel);

    // Add message to chat
    function addMessage(role, content) {
      messages.push({ role, content });
      renderMessages();
    }

    // Render messages
    function renderMessages() {
      messagesContainer.innerHTML = messages.map(msg => `
        <div class="hireology-widget-message ${msg.role}">
          ${escapeHtml(msg.content)}
        </div>
      `).join('');
      
      if (isLoading) {
        messagesContainer.innerHTML += `
          <div class="hireology-widget-typing">
            <span></span><span></span><span></span>
          </div>
        `;
      }
      
      messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    // Escape HTML to prevent XSS
    function escapeHtml(text) {
      const div = document.createElement('div');
      div.textContent = text;
      return div.innerHTML;
    }

    // Send message to AI
    async function sendMessage() {
      const message = input.value.trim();
      if (!message || isLoading) return;

      input.value = '';
      addMessage('user', message);
      isLoading = true;
      sendBtn.disabled = true;
      renderMessages();

      try {
        const response = await fetch(`${CONFIG.apiBaseUrl}/api/v1/chat/widget`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({
            agentId: CONFIG.agentId,
            sessionId: sessionId,
            message: message
          })
        });

        if (!response.ok) {
          throw new Error('Failed to get response');
        }

        const data = await response.json();
        addMessage('assistant', data.response || 'I apologize, I was unable to process that. Please try again.');
      } catch (error) {
        console.error('Hireology Widget Error:', error);
        addMessage('assistant', 'I apologize, there was an error processing your request. Please try again.');
      } finally {
        isLoading = false;
        sendBtn.disabled = false;
        renderMessages();
      }
    }

    // Event listeners for sending messages
    sendBtn.addEventListener('click', sendMessage);
    input.addEventListener('keypress', (e) => {
      if (e.key === 'Enter') {
        sendMessage();
      }
    });
  }

  // Initialize when DOM is ready
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
