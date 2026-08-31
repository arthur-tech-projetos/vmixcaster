<p align="center">
  <img src="https://raw.githubusercontent.com/arthur-tech-projetos/vmixcaster/main/Extens%C3%A3o-Browser/assets/logo-popup.png" alt="vMix Caster Logo" width="300">
</p>

<h1 align="center">VMIXCaster (PRO Broadcast)</h1>

<p align="center">
  <strong>Baixe clipes do YouTube em alta qualidade e gerencie intervalos de rádio diretamente na sua produção vMix.</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Vers%C3%A3o-1.5-blue.svg" alt="Version">
  <img src="https://img.shields.io/badge/Plataforma-Windows-lightgrey.svg" alt="Platform">
  <img src="https://img.shields.io/badge/Linguagem-C%23%20%7C%20.NET%208.0-512BD4.svg" alt="C#">
  <img src="https://img.shields.io/badge/Integra%C3%A7%C3%A3o-vMix%20API-4CAF50.svg" alt="vMix">
</p>

---

## 📡 O que é o VMIXCaster?

O **VMIXCaster** é uma solução de automação broadcast de ponta a ponta projetada para emissoras e web rádios. Ele cria uma ponte perfeita entre o seu navegador de internet e o software de produção ao vivo **vMix**. 

Com apenas um clique na extensão do Chrome/Brave, o servidor local baixa o vídeo desejado em altíssima resolução, processa o arquivo e injeta diretamente na lista de reprodução do vMix, aplicando transições de áudio e vídeo de forma autônoma.

## ✨ Recursos Principais

* **Download de Alta Qualidade Integrado:** Utiliza o motor `yt-dlp` sob o capô. O sistema verifica a ausência do motor e faz a auto-instalação silenciosa no primeiro uso.
* **Inteligência de Fila (Smart Queue):** O sistema detecta o que está no ar. Se a lista estiver vazia ou o clipe atual já tiver terminado, ele prepara o novo vídeo no ponto de corte. Se já houver algo rodando, ele empilha silenciosamente na fila para não atrapalhar a transmissão.
* **Modo Rádio (Automação Dupla):** Vigiando os cortes em tempo real, ele sorteia e dispara vinhetas de áudio e cartelas visuais perfeitamente sincronizadas ao final de cada videoclipe.
* **Atalhos Customizáveis (Global Hotkeys):** Ativação e desativação das automações (YouTube e Rádio) podem ser mapeadas livremente com combinações de teclas, sendo perfeito para integração com **Elgato Stream Deck** ou **Fifine Control Deck**.
* **Alta Performance e Estabilidade:** Conta com opção de "Auto-Start" invisível junto com o Windows, proteção contra travamentos, limpeza de memória RAM e geração de histórico em arquivos de Log diários (.txt).
* **Auto-Update Nativo:** Mantém-se sempre atualizado buscando e instalando silenciosamente as versões mais recentes nos Releases do GitHub.

## ⚙️ Arquitetura do Sistema

O projeto é dividido em duas partes que trabalham juntas:
1.  **Servidor C# (O Motor):** Fica oculto na bandeja do sistema (perto do relógio) escutando na porta `5050`. Gerencia downloads, lógica de automação, atalhos físicos e envia os comandos HTTP para a API do vMix.
2.  **Extensão de Navegador (O Controle):** Uma interface limpa (popup) injetada no navegador baseada em Chromium, que extrai as URLs e envia para o Servidor C# com um único clique (com feedback visual de sucesso/erro).

## 🚀 Como Instalar e Usar

### 1. Instalando o Servidor (Windows)
1. Vá até a aba [Releases](../../releases/latest) deste repositório e baixe o arquivo executável mais recente (`VMIXCaster.exe`).
2. Execute o aplicativo. Ele criará a pasta raiz no `C:\VMIXCaster`, fará o download automático do motor de vídeo e aparecerá perto do relógio do Windows.

### 2. Instalando a Extensão (Navegador)
1. Baixe o arquivo `.zip` da Extensão na mesma página de Releases e extraia no seu PC.
2. No Google Chrome, Edge ou Brave, acesse a página de extensões (ex: `chrome://extensions/`).
3. Ative o **Modo do desenvolvedor** no canto superior direito.
4. Clique em **"Carregar sem compactação"** e selecione a pasta extraída.

### 3. Configuração Inicial
1. Dê um duplo clique no ícone azul do VMIXCaster perto do relógio do Windows.
2. Vá até a aba **Configurações**.
3. Defina os números exatos dos **Inputs** correspondentes no seu vMix (Ex: Lista do YT, Cartela Visual, Grade de Músicas).
4. Configure os **Atalhos do Teclado** se for utilizar um controlador físico.
5. Selecione se deseja iniciar automaticamente com o Windows e clique em **Guardar Todas as Configurações**.

## 💻 Requisitos do Sistema
* Windows 10 ou superior.
* vMix instalado e rodando com a funcionalidade de Web Controller ativada.
* Navegador baseado em Chromium (Chrome, Brave, Edge) para a extensão.

---

<p align="center">
  Desenvolvido para profissionais de broadcast que precisam de agilidade e estabilidade no ar.
</p>
