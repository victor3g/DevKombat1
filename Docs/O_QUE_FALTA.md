# O que já foi extraído e o que falta

## 1. Extraído dos repositórios (`Tools/extrair_assets_mk.py`)

Fontes:

- [perninio/Mortal-KombatII-remake](https://github.com/perninio/Mortal-KombatII-remake): sprites do MK2 SNES, sangue, menu e sons.
- [uzeernabeel/Mortal-Kombat-Reimagined](https://github.com/uzeernabeel/Mortal-Kombat-Reimagined): sons do MK3, efeitos e cenário.
- [CatfishStudio/Mortal-Kombat-QC](https://github.com/CatfishStudio/Mortal-Kombat-QC): 12 cenários.
- [nazimboudeffa/phaser2-mk1](https://github.com/nazimboudeffa/phaser2-mk1), [NeMoDDD/Mortal-Kombat](https://github.com/NeMoDDD/Mortal-Kombat) e [vladimirplyukhin89/MortalKombat](https://github.com/vladimirplyukhin89/MortalKombat): mais 3 cenários.

| Tipo | Quantidade | Detalhes |
|---|---:|---|
| Corpo base (ninja MK2) | 38 animações | Parado, andar, agachar, defender, pular, mortal, 2 socos, 2 chutes, joelhada, chute giratório, rasteira, gancho, golpes agachado e no ar, voadora, arremesso, ser arremessado, 3 reações de dano, derrubado, jogado para o alto, levantar, tonto, 2 vitórias, derrota, tirar a máscara e bebê. Separado em **corpo + roupa tingível**: dá para fazer qualquer cor, como os ninjas do MK. |
| Efeitos | 30 | Lança e corrente, bola de fogo e explosão do Scorpion, rajada e chão de gelo, bolas de fogo do Liu Kang, bola e impacto de sombra, onda do Jax, sais, leque e arco, lança verde, 9 tipos de sangue (jato, spray, gotas, explosão, chafariz, poça...), 66 pedaços de ossos e carne, corpo pegando fogo, explosão, fogo do inferno, gelo quebrando, portal. |
| Interface | 25 | FIGHT!, FINISH HIM/HER (vermelho e amarelo), ROUND, FATALITY (2 versões), BABALITY, FLAWLESS VICTORY, TIME IS UP, GAME OVER, TOASTY e o rosto, medalha de vitória, logos do dragão, painéis de pedra, tela de VS e botões. |
| Cenários | 17 | Arena do Kahn, Templo Shaolin, Sala do Trono, Arsenal, Covil do Scorpion, Metrô, Rua, Banco, Torre do Sino, Masmorra, Câmara das Almas, Templo do Kombate, Cemitério, Câmara Verde, Poço de Lava, Trono Sombrio e Terraço. |
| Locutor | 22 | Round 1–4, Fight, Finish Him, Flawless Victory, Excellent, Outstanding, Superb, Well Done, Brutality, risadas, You Suck, Pathetic, Toasty, Frosty, gongo. |
| Sons de golpes | 54 | Impactos. |
| Vozes | 136 | 97 masculinas e 39 femininas (gemidos e gritos). |
| Especiais | 9 | Get Over Here, Come Here, gelo, impacto forte, "crispy", gritos de Brutality. |
| Músicas | 16 | Título, seleção, uma de luta e 13 trechos do MK3. |
| Fonte | 1 | Fonte do MK2 com acentos. |

## 2. O que já funciona no código

- **Partida:** melhor de 3 rounds, apresentação ROUND N e FIGHT!, relógio de 99 s,
  TIME IS UP, empate, FLAWLESS VICTORY e elogios do locutor.
- **Finalização:** FINISH HIM/HER com 7 s para a Fatality ou a Babality.
  Cinco finalizações: fogo, gelo, explosão, gancho e babality.
- **Golpes do MK2:**
  - 2 socos, 2 chutes, gancho que lança, rasteira e chute giratório.
  - Golpes agachado, golpes no ar e voadora.
  - Arremesso.
  - Defesa em pé e agachado, com dano raspado dos especiais.
  - Combos no ar (até 4 golpes, com dano menor a cada um).
  - Contador de HITS, travamento de impacto e tremor de tela.
- **Especiais:** 24 habilidades do MK prontas e comandos escritos em português.
  - O comando pode ser feito logo depois de um golpe normal, que é cancelado.
  - Projéteis se anulam quando se encontram.
  - Só um projétil por lutador na tela.
- **Barra especial e golpe supremo.**
- **CPU:**
  - Se aproxima e defende.
  - Usa gancho contra pulo e projéteis de longe.
  - Faz combos e tenta Fatality.
  - Usa os mesmos comandos do jogador.
- **Modos:** 1 jogador contra a CPU, 2 jogadores no mesmo teclado e treino.
- **Telas:** título, seleção de lutador (com o lado da CPU sorteando), seleção de arena (ou aleatória),
  VS, HUD no estilo MK2 e pausa com a lista de golpes de cada lutador.
- **Detalhes:** sangue com gotas que caem, ossos voando, sombra no chão, câmera que acompanha
  e Toasty! aleatório no gancho.
- **Editor:**
  - Preparação automática da cena.
  - Assistente "Novo Lutador".
  - Importação dos sprites pelo nome do arquivo.
  - Lista de lutadores atualizada sozinha.
- **Mariana** já está ligada:
  - Coffee Rush é um projétil cor de café.
  - Git Push --force é um teleporte.
  - Kernel Panic é o supremo.
  - A Fatality é "Deploy na Sexta-Feira".

## 3. Falta para ficar completo — assets

| Item | Situação | Como resolver |
|---|---|---|
| **Sprites dos amigos** | Só a Mariana tem imagens, e só 8 poses paradas (uma por animação). Os demais usam o ninja tingido. | Desenhar ou gerar por IA de 3 a 8 quadros por animação, no mesmo tamanho e com os pés na mesma linha. Prioridade: idle, walk, punch, kick, hit, knockdown, victory. |
| **Retratos da seleção** | Não há; o jogo mostra o ninja/idle. | Um retrato por amigo (Selection Portrait). |
| **Efeitos próprios dos amigos** | Usam os efeitos do MK com outra cor (o café da Mariana é a bola de fogo pintada). | Quadros próprios em Effect Frames de cada golpe. |
| **Vozes dos amigos** | Nenhuma. O jogo usa gemidos do MK3. | Gravar entrada, ataque, dano, vitória e o nome do lutador para o locutor. |
| **Locutor: "Fatality", "Finish Her", "Babality"** | Não vieram nos repositórios. Hoje toca "Finish Him" para os dois e uma risada no lugar de "Fatality". | Gravar ou buscar e salvar como `Audio/Announcer/fatality.mp3` e `finish_her.mp3`. |
| **Som de golpe no ar e de defesa** | Não há som de "whoosh". A defesa usa um impacto mais agudo. | Os 161 sons em `Audio/Unsorted` (MK3) não têm nome; é preciso ouvir e separar. |
| **Cenários** | 12 deles têm só 670x230 pixels e são estáticos. Os do MK9 são fotos em alta e destoam do pixel art. | Escolher um estilo; MK2 tinha cenários com camadas e animação (parallax). |
| **Fatality de cenário** (The Pit, espinhos) | Não existe. | Precisa de arte e código. |
| **Chefes** (Goro, Kintaro, Shao Kahn) | Não existem. | Sprites e IA própria. |
| **Licença** | Tudo o que foi extraído é da Warner Bros./NetherRealm. | Uso só local. Para publicar ou distribuir, substitua por arte e sons próprios. |

## 4. Falta para ficar completo — código e jogo

### Precisa ser feito primeiro

1. **Abrir no Unity e testar.** O código compila contra as DLLs do Unity e os comandos de
   especiais passaram nos testes automáticos. Mesmo assim, **ninguém jogou ainda**: o Unity
   não está instalado nesta máquina. Pode haver ajustes de tamanho, posição e tempo de
   animação (por exemplo, a altura do chão de cada cenário em `stages.json`).
2. **Padronizar as imagens da Mariana.** As poses foram geradas em tamanhos diferentes:
   - parada e andando têm cerca de 1460 px de altura;
   - soco e chute têm cerca de 1160 px.

   Por isso ela encolhe uns 20% ao socar e chutar. Refaça as poses no mesmo enquadramento
   (mesma altura do corpo e pés na base da imagem). A sobra transparente embaixo é pequena
   (2–3%) e não atrapalha.

### Jogabilidade

- **Equilíbrio:** dano, alcance e velocidade dos golpes estão em `KombatMoves.cs` e foram
  estimados a partir do MK2; precisam de teste.
- **Hitboxes por quadro.** Hoje cada golpe tem uma caixa fixa durante a parte ativa.
- **Mecânicas:**
  - Botão de corrida e combos encadeados (do MK3).
  - Arremesso no ar.
  - Virar de costas durante o pulo.
- **Finalizações:** Friendship, Animality, Brutality e Fatality de cenário.
- **CPU:** níveis de dificuldade no menu (hoje é um valor fixo na cena) e chefe final.

### Modos e menus

- **Modo arcade** com a torre de oponentes, chefes, finais de cada personagem, continue e recordes.
- **Opções:**
  - volume;
  - dificuldade;
  - sangue ligado ou desligado;
  - número de rounds;
  - tempo;
  - **troca de teclas** e **suporte a controle (gamepad)**, que hoje é só teclado.
- **Intros e vitórias animadas** por personagem.

### Plataforma

- **Build e distribuição:**
  - Testar o build WebGL.
  - Controles de toque para celular.
  - Ícone e tela de carregamento.

## 5. Depois da adaptação completa dos amigos

Quando cada amigo tiver ficha, sprites, retrato, efeitos e vozes, ainda faltarão:

1. Testar o equilíbrio entre todos os lutadores (dano, alcance, especiais muito fortes).
2. Revisar se cada comando de especial é confortável e se nenhum conflita com outro do mesmo lutador.
3. Colocar o nome de cada amigo na voz do locutor e trocar as vozes do MK pelas gravadas.
4. Ajustar a CPU para cada estilo (zoner usa mais projéteis, grappler mais agarrões).
5. Trocar todos os assets do MK por arte e sons próprios, se o jogo for publicado ou
   compartilhado fora do grupo de amigos.
6. Fazer o build final (Windows ou WebGL) e testar em outra máquina.
