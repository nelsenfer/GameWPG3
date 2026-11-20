// Prolog: Chat HP di awal game.
// Di awal, hanya ada chat dari Bos di daftar kontak.
// Setelah obrolan dengan Bos selesai, barulah pesan baru dari Mama muncul di daftar kontak.
-> hub_bos

=== hub_bos ===
* [Bos::Lembur dulu. Bersyukur kamu masih punya pekerjaan di sini.::3]
    -> bos

=== bos ===
Bos: Kamu masih di kantor kan? #hp
Bos: Laporan harus selesai besok pagi ya. #hp
Bos: Lembur dulu. Bersyukur kamu masih punya pekerjaan di sini. #hp
* [Siap, Pak.]
    MC: Siap, Pak. Saya kerjakan malam ini. #hp
* [Saya usahakan, Pak.]
    MC: Saya usahakan, Pak. #hp
- -> hub_mama

=== hub_mama ===
* [Mama::Jangan lupa transfer buat Mama sama adik kembarmu ya.::1]
    -> mama

=== mama ===
Mama: Jangan lupa transfer buat Mama sama adik kembarmu ya. #hp
* [Iya, Ma. Nanti aku transfer.]
    MC: Iya, Ma. Nanti aku transfer. #hp
* [Nanti ya, Ma. Aku lagi lembur.]
    MC: Nanti ya, Ma. Aku lagi lembur. #hp
- -> END
