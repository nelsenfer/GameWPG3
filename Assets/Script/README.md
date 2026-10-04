# Game Scripts

Struktur script:

```text
Script/
|-- Core/
|   |-- Events/
|   |-- Interfaces/
|   |-- Interaction/
|   |-- Player/
|   `-- UI/
|-- Minigames/
|   `-- Typing/
`-- Fulfillment/
    `-- Examples/
```

## Setup singkat

1. Buat asset `Create > Game > Config > Player Movement Config`.
2. Isi `Move Speed`, `Acceleration`, `Deceleration`, `Interaction Radius`, dan layer interactable.
3. Tambahkan `Rigidbody2D`, `PlayerController`, dan `InteractionDetector` ke Player.
4. Buat asset `Create > Game > Events > Interaction Event Channel`, lalu assign asset yang sama ke detector dan `InteractionPromptUI`.
5. Tambahkan collider trigger dan `CandleInteractable` ke objek lilin. Pastikan layer objek termasuk di `interactableLayer`.

Project ini memakai Unity 6, sehingga `PlayerController` menggunakan `Rigidbody2D.linearVelocity`.

## Minigame mengetik

Script minigame mengetik berada di `Minigames/Typing/`:

- `TypingTaskData`: asset task yang dibuat melalui `Create > Game > Typing > Typing Task Data`.
- `TypingMinigameController`: mengatur panel UI, input ketikan, timer, dan hasil.
- `TypingTaskInteractable`: menghubungkan objek yang bisa diinteraksi dengan controller dan event penyelesaian.
