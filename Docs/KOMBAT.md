# Dev Kombat — base estilo Mortal Kombat

Base jogável no estilo do **Mortal Kombat 2**. Ela traz rounds, FIGHT!, tempo de 99 s,
FINISH HIM/HER, Fatality, Babality, sangue, golpes do MK2 e CPU. Os lutadores são os seus
amigos, cada um com as habilidades de um personagem do MK.

## 1. Primeira execução

1. **Extraia os assets do MK** (cenários, efeitos, interface e sons). Na pasta do projeto:

   ```bash
   python -m pip install pillow
   python Tools/extrair_assets_mk.py
   ```

   O script clona os repositórios de referência em `../_referencias` e grava tudo em
   `Assets/Resources/Kombat`. Essa pasta fica **fora do Git** de propósito, porque os
   arquivos são da Warner Bros./NetherRealm. Sem ela o jogo também roda, mas com formas
   simples e sem som.
2. Abra o projeto no **Unity 6** (`6000.0.30f1`).
3. Na primeira abertura, o menu **Dev Kombat > Preparar Kombat (estilo MK)** roda sozinho.
   Ele cria `Assets/Core/Scenes/KombatArena.unity` e a lista `Assets/Core/Data/KombatRoster.asset`.
4. Abra **KombatArena** e aperte **Play**.

## 2. Controles

| Ação | Jogador 1 | Jogador 2 |
|---|---|---|
| Mover / pular / agachar | W A S D | Setas |
| Soco fraco / forte | J / U | Num1 / Num4 |
| Chute fraco / forte | K / I | Num2 / Num5 |
| Defesa | L ou Espaço | Num3 ou Num0 |
| Pausa e lista de golpes | Esc | Esc |

**Golpes do MK2 para todos:**

- Gancho: Baixo + Soco Forte. Joga o oponente para o alto e permite combos no ar.
- Rasteira: Trás + Chute Fraco.
- Chute giratório: Trás + Chute Forte.
- Arremesso: perto + Frente + Soco Fraco.
- Golpes agachado, socos e chutes no ar e voadora.
- Defesa: em pé defende tudo. Agachado não defende golpes vindos do pulo.

**Finalizações:** quando aparecer FINISH HIM/HER, fique perto e faça o comando:

- Fatality: o comando de cada lutador. Se o lutador não tiver um, vale
  `Frente, Frente, Baixo + Soco Forte`.
- Babality: `Baixo, Baixo, Baixo + Chute Fraco` para todos.

**Modo Treino:**

- F1 deixa o boneco tonto para testar Fatality e Babality.
- F2 faz o boneco defender.
- A barra especial fica sempre cheia.

## 3. Criando um amigo

1. Preencha a ficha em `Fichas/` (modelo: `Fichas/FichaBase.md`).
2. No Unity, abra **Dev Kombat > Novo Lutador (amigo)...**:
   - Digite o nome, o apelido e a cor.
   - Escolha **de qual personagem do MK ele herda as habilidades**.
   - O assistente cria `Assets/Fighters/<Nome>/` com `<Nome>_Data.asset`, as pastas
     `Sprites/` e `Audio/` e a ficha em `Fichas/<Nome>/`. O lutador já entra na tela de seleção.
3. Selecione o `<Nome>_Data.asset` e personalize:
   - **Golpes especiais:** em cada um você pode mudar
     - o nome do golpe;
     - o comando, em português, como `Baixo, Frente + Soco`;
     - o dano;
     - a fala que aparece no balão;
     - a **habilidade do MK**, que define o que o golpe faz;
     - os quadros próprios do efeito (por exemplo, uma caneca no lugar da bola de fogo);
     - a cor do efeito;
     - um som próprio.
   - **Golpe supremo** (Ultimate Move): só sai com a barra especial cheia.
   - **Fatality:** escolha uma das finalizações da lista e o comando.
   - **Vida, velocidade, pulo, dano de soco e de chute**, que ajustam os golpes normais.
   - **Áudio:** falas de entrada, de ataque, de dano e de vitória, mais o locutor dizendo o nome.
   - **Diálogos:** a primeira fala aparece no início da luta e a última na vitória.
   - **Personagem feminina:** ativa o FINISH HER e as vozes femininas.
4. **Sprites:** coloque os PNGs em `Assets/Fighters/<Nome>/Sprites/`, com fundo transparente,
   e use o nome da animação no arquivo:

   `spr_<nome>_idle_01.png`, `spr_<nome>_idle_02.png`, `spr_<nome>_walk_01.png`,
   `spr_<nome>_punch_01.png`, `spr_<nome>_kick_01.png`, `spr_<nome>_hit_01.png`...

   Animações aceitas: `idle walk crouch jump block punch kick uppercut sweep special grab
   hit knockdown dizzy victory defeat taunt`.

   Depois rode **Dev Kombat > Importar sprites dos lutadores**. Se faltar alguma animação,
   o jogo usa a mais parecida. Sem nenhum sprite, o amigo aparece como o ninja do MK2
   pintado com a cor dele, como os ninjas do MK.
5. **Retrato:** arraste uma imagem para **Selection Portrait** no FighterData.

## 4. Habilidades do MK disponíveis

| Grupo | Habilidade | Original | O que faz |
|---|---|---|---|
| Projétil | Bola de fogo alta / baixa | Liu Kang | Derruba; a baixa só se defende agachado |
| Projétil | Bola de gelo | Sub-Zero | Congela por 2 s |
| Projétil | Lança (Get over here!) | Scorpion | Puxa e deixa tonto |
| Projétil | Bola de sombra | Johnny Cage | Dano |
| Projétil | Onda de energia | Jax | Derruba |
| Projétil | Sai | Mileena | Dano rápido |
| Projétil | Leque bumerangue | Kitana/Jade | Vai e volta |
| Projétil | Raio / Lança verde | Raiden / Shao Kahn | Derruba |
| Teleporte | Com soco / com chute / simples | Scorpion / Mileena / Raiden | Reaparece atrás do oponente |
| Investida | Deslizada | Sub-Zero | Rasteira longa |
| Investida | Chute sombra | Johnny Cage | Avanço com rastro verde |
| Investida | Voadora / Bicicleta | Liu Kang | Atravessa a tela / vários golpes |
| Investida | Torpedo | Raiden | Voa de cabeça |
| Investida | Rolamento | Mileena | Rola e derruba |
| Corpo a corpo | Soco no chão | Jax | Tremor que derruba quem está no chão |
| Corpo a corpo | Agarrão com socos | Jax | Vários socos e derruba |
| Corpo a corpo | Leque que levanta | Kitana | Deixa o oponente indefeso no ar |
| Corpo a corpo | Golpe baixo | Johnny Cage | Atordoa |
| Supremo | Explosão com tela azul | — | Barra cheia; dano alto |
| Fatality | Fogo e esqueleto | Scorpion | Toasty! |
| Fatality | Congelar e despedaçar | Sub-Zero | |
| Fatality | Explodir em pedaços | Kano/Jax | |
| Fatality | Gancho com chuva de sangue | — | |
| Fatality | Babality | — | Vira bebê |

A tela de seleção também mostra **8 modelos prontos** (Ninja Amarelo/Scorpion,
Ninja Azul/Sub-Zero, Monge Dragão/Liu Kang, Astro de Cinema/Johnny Cage, Sargento/Jax,
Princesa do Leque/Kitana, Assassina de Sai/Mileena e Deus do Trovão/Raiden). Para
escondê-los, desmarque **Include Mk Templates** em `KombatRoster.asset`.

## 5. Onde fica cada coisa

| Pasta/arquivo | Conteúdo |
|---|---|
| `Assets/Core/Scripts/Kombat/` | Motor de luta (lutador, partida, CPU, projéteis, efeitos, interface) |
| `Assets/Core/Editor/` | Preparação da cena, assistente de novo lutador, importação de assets |
| `Assets/Resources/Kombat/` | Assets extraídos do MK (fora do Git) |
| `Tools/extrair_assets_mk.py` | Extração e recorte dos assets |
| `Docs/O_QUE_FALTA.md` | O que falta para o jogo ficar completo |
