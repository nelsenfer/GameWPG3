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
