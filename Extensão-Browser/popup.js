document.getElementById('sendBtn').addEventListener('click', () => {
    const quality = document.getElementById('quality').value;
    const btn = document.getElementById('sendBtn');
    
    // Salva o HTML original do botão
    const originalBtnHTML = btn.innerHTML;
    
    // Altera o botão para o estado de loading com um ícone de spinner animado
    btn.innerHTML = `<svg class="spinner" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" style="animation: spin 1s linear infinite;"><line x1="12" y1="2" x2="12" y2="6"></line><line x1="12" y1="18" x2="12" y2="22"></line><line x1="4.93" y1="4.93" x2="7.76" y2="7.76"></line><line x1="16.24" y1="16.24" x2="19.07" y2="19.07"></line><line x1="2" y1="12" x2="6" y2="12"></line><line x1="18" y1="12" x2="22" y2="12"></line><line x1="4.93" y1="19.07" x2="7.76" y2="16.24"></line><line x1="16.24" y1="7.76" x2="19.07" y2="4.93"></line></svg> PROCESSANDO...`;
    btn.disabled = true;

    chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
        let currentUrl = tabs[0].url;
        
        // Validação da URL
        if (!currentUrl.includes('youtube.com') && !currentUrl.includes('youtu.be')) {
            updateStatus('Erro: Guia atual não é um vídeo do YouTube.', 'error');
            restoreButton(btn, originalBtnHTML);
            return;
        }

        updateStatus('Conectando ao servidor local...', 'waiting');

        // Dispara o POST para o programa em C#
        fetch('http://localhost:5050/enviar', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ url: currentUrl, quality: quality })
        })
        .then(response => response.text())
        .then(data => {
            updateStatus('Comando recebido! Baixando no servidor...', 'success');
            restoreButton(btn, originalBtnHTML);
        })
        .catch(error => {
            updateStatus('Falha de conexão. O VMIXCaster (C#) está aberto?', 'error');
            restoreButton(btn, originalBtnHTML);
        });
    });
});

// Função para atualizar o painel de status dinamicamente
function updateStatus(message, type) {
    const statusMsg = document.getElementById('statusMsg');
    const statusIcon = document.getElementById('statusIcon');
    
    statusMsg.innerText = message;
    statusIcon.className = 'status-icon'; // Limpa as classes de cor anteriores
    
    if (type === 'waiting') {
        statusIcon.classList.add('status-waiting');
        statusMsg.style.color = '#d97706'; // Laranja escuro para texto
    } else if (type === 'success') {
        statusIcon.classList.add('status-success');
        statusMsg.style.color = '#059669'; // Verde escuro para texto
    } else if (type === 'error') {
        statusIcon.classList.add('status-error');
        statusMsg.style.color = '#dc2626'; // Vermelho escuro para texto
    }
}

// Função para restaurar o botão após o disparo
function restoreButton(btn, originalHTML) {
    setTimeout(() => {
        btn.innerHTML = originalHTML;
        btn.disabled = false;
    }, 1500); // Aguarda 1.5s antes de destravar para evitar múltiplos cliques
}

// Injeta o keyframe de animação para o ícone de carregamento girar
const styleSheet = document.createElement('style');
styleSheet.innerHTML = `@keyframes spin { 100% { transform: rotate(360deg); } }`;
document.head.appendChild(styleSheet);