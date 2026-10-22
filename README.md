# GSM CHECK TOOL

Ferramenta Windows para diagnóstico informativo de aparelhos Android e iPhone conectados por USB, com interface azul, branca e preta.

## Download do executável
Abra **Actions** e selecione o workflow **Build GSM CHECK TOOL Windows**. Quando estiver concluído com sucesso, baixe o artefato `GSM-CHECK-TOOL-Windows-x64` e extraia o ZIP. Execute `GSM_CHECK_TOOL.exe` mantendo a pasta `tools` junto dele.

## Conectar aparelho
- Android: use cabo USB de dados, habilite Depuração USB quando necessário, instale o driver USB do fabricante e autorize o computador no aparelho.
- iPhone: instale Apple Devices ou iTunes para suporte USB, conecte com cabo de dados, desbloqueie e toque em Confiar neste computador.

## Limitações
IMEI e outros identificadores podem ser bloqueados por permissões do sistema. O iOS pode não expor saúde da bateria ou alguns detalhes por USB. O programa indica quando uma informação não está disponível e não inventa valores.

O workflow baixa ferramentas de terceiros para conexão USB: Android Platform Tools e libimobiledevice-windows. Revise suas origens antes de executar em um computador de produção.