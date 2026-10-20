# Dev Kombat — protótipo 1v1

> **Novo: base completa estilo Mortal Kombat.** Rounds, FINISH HIM, Fatality, sangue,
> 24 habilidades do MK2 para dar aos amigos, CPU, 2 jogadores e treino.
> Veja [Docs/KOMBAT.md](Docs/KOMBAT.md) para rodar e criar lutadores e
> [Docs/O_QUE_FALTA.md](Docs/O_QUE_FALTA.md) para o que ainda falta.
> Resumo: rode `python Tools/extrair_assets_mk.py`, abra o projeto e dê Play em
> `Assets/Core/Scenes/KombatArena.unity`.
>
> O protótipo antigo da Mariana, descrito abaixo, continua em `PrototypeArena`.

Projeto Unity 2D com a personagem Mariana configurada para o primeiro teste contra a CPU.

## Abrir pela primeira vez

1. Instale o Unity `6000.0.30f1` ou outra versão Unity 6 compatível pelo Unity Hub.
2. No Hub, use **Add project from disk** e selecione esta pasta.
3. Aguarde a importação e a compilação dos scripts.
4. A configuração automática importará os sprites, preencherá o prefab e criará
   `Assets/Core/Scenes/PrototypeArena.unity`.
5. Abra essa cena e pressione **Play**.

Se a cena não for criada automaticamente, execute o menu:

`Dev Kombat > Preparar Primeiro Teste`

## Controles

- Jogador: `A/D` para mover, `W` para pular, `F` para socar e `G` para chutar.
- O segundo lutador é controlado pelo computador.
- Reiniciar round: `R`.

O protótipo possui movimento, pulo, soco, chute, dano, derrota, vitória, câmera,
chão, limites de arena, barras de vida e uma IA básica. Jogador e CPU usam Mariana
até que um segundo lutador seja criado.

## Build para navegador

Instale o módulo **WebGL Build Support** pelo Unity Hub e use:

`Dev Kombat > Gerar Build WebGL de Teste`

O resultado será criado em `Builds/WebGL`. A pasta `Builds` é ignorada pelo Git.
