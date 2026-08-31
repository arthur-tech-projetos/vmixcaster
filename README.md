<p align="center">
  <img src="https://raw.githubusercontent.com/arthur-tech-projetos/vmixcaster/main/Extens%C3%A3o-Browser/assets/logo-popup.png" alt="vMix Caster Logo" width="300">
</p>

<h1 align="center">VMIXCaster (PRO Broadcast)</h1>

<p align="center">
  <strong>Transfira videoclipes do YouTube em alta qualidade e faça a gestão de intervalos de rádio diretamente na sua produção vMix.</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Vers%C3%A3o-1.6-blue.svg" alt="Version">
  <img src="https://img.shields.io/badge/Plataforma-Windows-lightgrey.svg" alt="Platform">
  <img src="https://img.shields.io/badge/Linguagem-C%23%20%7C%20.NET%208.0-512BD4.svg" alt="C#">
  <img src="https://img.shields.io/badge/Integra%C3%A7%C3%A3o-vMix%20API-4CAF50.svg" alt="vMix">
</p>

---

##  O que é o VMIXCaster?

O **VMIXCaster** é uma solução de automação broadcast de ponta a ponta projetada para emissoras e web rádios. Ele cria uma ponte perfeita entre o seu navegador de internet e o software de produção ao vivo **vMix**. 

Com apenas um clique na extensão do Chrome/Brave, o servidor local transfere o vídeo desejado em altíssima resolução, processa o ficheiro e injeta-o diretamente na lista de reprodução do vMix, aplicando transições de áudio e vídeo de forma totalmente autónoma.

##  Recursos Principais

* **Transferência Ultra-Rápida (Ultra-Speed):** Utiliza o motor `yt-dlp` com 16 ligações simultâneas. Traz otimização inteligente para descarregar ficheiros 720p nativos de forma quase instantânea ou 1080p de alta qualidade. O sistema instala e atualiza o motor automaticamente no primeiro uso.
* **Feedback Visual em Tempo Real:** A extensão do navegador possui uma barra de progresso inteligente que comunica com o servidor C# a cada 0.5 segundos, mostrando o progresso da transferência e o estado do processamento sem que o operador precise de adivinhar o que está a acontecer.
* **Inteligência de Fila (Smart Queue):** O sistema deteta o que está no ar no vMix. Se a lista estiver vazia ou o clipe atual tiver terminado, ele prepara o novo vídeo no ponto de corte. Se já houver algo a decorrer, ele empilha silenciosamente na fila para não atrapalhar a emissão.
* **Modo Rádio (Automação Dupla):** Vigiando os cortes em tempo real, ele sorteia e dispara vinhetas de áudio e cartelas visuais perfeitamente sincronizadas ao final de cada videoclipe.
* **Atalhos Personalizáveis (Global Hotkeys):** A ativação e desativação das automações (YouTube e Rádio) podem ser mapeadas livremente com combinações de teclas, sendo o sistema perfeito para integração total com **Elgato Stream Deck** ou **Fifine Control Deck**.
* **Alta Performance e Estabilidade:** Conta com a opção de "Auto-Start" invisível juntamente com o Windows, proteção inteligente de memória RAM (evitando o congelamento de atalhos) e geração de histórico em ficheiros de Log diários (`.txt`).
* **Auto-Update Nativo:** Mantém-se sempre atualizado, procurando e instalando silenciosamente (100% seguro, sem ficheiros `.bat`) as versões mais recentes nos Releases do GitHub.

##  Arquitetura do Sistema

O projeto é dividido em duas partes que trabalham em conjunto:
1.  **Servidor C# (O Motor):** Fica oculto na bandeja do sistema (junto ao relógio) a escutar na porta `5050`. Gere as transferências, a lógica de automação, os atalhos físicos e envia os comandos HTTP para a API do vMix.
2.  **Extensão de Navegador (O Controlo):** Uma interface limpa (popup) injetada no navegador baseado em Chromium, que extrai os URLs e os envia para o Servidor C# com um único clique (agora com painel de progresso visual dinâmico).

##  Como Instalar e Usar

### 1. Instalar o Servidor (Windows)
1. Vá até à aba [Releases](../../releases/latest) deste repositório e descarregue o ficheiro executável mais recente (`VMIXCaster.exe`).
2. Execute a aplicação. Ela criará a pasta raiz em `C:\VMIXCaster`, fará a transferência automática do motor de vídeo e aparecerá perto do relógio do Windows.

### 2. Instalar a Extensão (Navegador)
1. Descarregue o ficheiro `.zip` da Extensão na mesma página de Releases e extraia-o no seu PC.
2. No Google Chrome, Edge ou Brave, aceda à página de extensões (ex: `chrome://extensions/`).
3. Ative o **Modo de programador** no canto superior direito.
4. Clique em **"Carregar sem compactação"** e selecione a pasta extraída.

### 3. Configuração Inicial
1. Dê um duplo clique no ícone azul do VMIXCaster perto do relógio do Windows.
2. Vá até à aba **Configurações**.
3. Defina os números exatos dos **Inputs** correspondentes no seu vMix (Ex: Lista do YT, Cartela Visual, Grade de Músicas).
4. Configure os **Atalhos do Teclado** se for utilizar um controlador físico.
5. Selecione se deseja iniciar automaticamente com o Windows e clique em **Guardar Todas as Configurações**.

##  Requisitos do Sistema
* Windows 10 ou superior.
* vMix instalado e a correr com a funcionalidade *Web Controller* ativada.
* Navegador baseado em Chromium (Chrome, Brave, Edge) para a extensão.

---

<p align="center">
  Desenvolvido para profissionais de broadcast que precisam de agilidade e estabilidade no ar.
</p>
