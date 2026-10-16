# Primeiro teste da Mariana

O protótipo aceita uma ou várias imagens em cada animação. Com apenas uma imagem,
o estado fica estático; com várias, elas são reproduzidas na ordem da lista.

## Preparação das imagens

1. Gere as poses da Mariana por IA com fundo transparente.
2. Importe os PNGs para a pasta `Sprites`.
3. No Inspector de cada imagem, escolha `Texture Type: Sprite (2D and UI)`.
4. Para pixel art, use `Filter Mode: Point` e `Compression: None`.

## Configuração do prefab

1. Abra `Mariana.prefab`.
2. No componente `Prototype Fighter 2D`, ajuste o tamanho das listas.
3. Arraste as imagens para `Idle Frames`, `Walk Frames`, `Jump Frames`,
   `Punch Frames`, `Kick Frames`, `Hit Frames`, `Defeat Frames` e `Victory Frames`.
4. Se ainda não houver uma pose, use a imagem de Idle naquele campo ou deixe-o
   vazio; o código usa Idle automaticamente como substituição.

## Cena de teste

1. Crie uma cena 2D.
2. Adicione um chão com `BoxCollider2D`.
3. Arraste duas cópias de `Mariana.prefab` para a cena.
4. Na segunda cópia, altere as teclas no componente para evitar conflito.
5. Posicione os lutadores sobre o chão e pressione Play.

Controles padrão da primeira cópia:

- `A` e `D`: andar.
- `W`: pular.
- `F`: socar.
- `G`: chutar.

O script cria automaticamente `SpriteRenderer`, `Rigidbody2D` e
`CapsuleCollider2D` quando esses componentes ainda não existirem.
