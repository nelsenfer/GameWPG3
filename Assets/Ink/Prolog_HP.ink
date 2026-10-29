// Prolog: HP dengan 2 kontak. Pemain memilih kontak mana dulu.
// Daftar kontak = pilihan berformat [ID::cuplikan chat::jumlah belum dibaca]
// ID = Character ID di CharacterData (sama persis, huruf besar-kecil).
-> hub

=== hub ===
* [Bos::Lembur dulu. Bersyukur kamu masih punya pekerjaan di sini.::3]
    -> bos
* [Mama::Jangan lupa transfer buat Mama sama adik kembarmu ya.::1]
    -> mama
* ->
    -> END

=== bos ===
Bos: Kamu masih di kantor kan? #hp
Bos: Laporan harus selesai besok pagi ya. #hp
Bos: Lembur dulu. Bersyukur kamu masih punya pekerjaan di sini. #hp
* [Siap, Pak.]
    MC: Siap, Pak. Saya kerjakan malam ini. #hp
* [Saya usahakan, Pak.]
    MC: Saya usahakan, Pak. #hp
- -> hub

=== mama ===
Mama: Jangan lupa transfer buat Mama sama adik kembarmu ya. #hp
* [Iya, Ma. Nanti aku transfer.]
    MC: Iya, Ma. Nanti aku transfer. #hp
* [Nanti ya, Ma. Aku lagi lembur.]
    MC: Nanti ya, Ma. Aku lagi lembur. #hp
- -> hub
