# common-passwords.txt

Liste häufig verwendeter Passwörter, die die Passwortrichtlinie ablehnt (ein Eintrag pro Zeile, kleingeschrieben,
nur Einträge mit mindestens 12 Zeichen – kürzere Passwörter scheitern ohnehin an der Mindestlänge).
Wird beim Build in die Assembly eingebettet; es findet keine Netzwerkanfrage statt.

- Quelle: `Passwords/Common-Credentials/100k-most-used-passwords-NCSC.txt` aus
  [SecLists](https://github.com/danielmiessler/SecLists) (MIT-Lizenz, Copyright (c) 2018 Daniel Miessler)
- Bearbeitung: auf Kleinbuchstaben gebracht, auf mindestens 12 Zeichen gefiltert, Duplikate entfernt, sortiert
- Die Daten stammen ursprünglich vom britischen National Cyber Security Centre (NCSC); deren Lizenzbedingungen
  wurden nicht geprüft. Vor einer Veröffentlichung des Repos prüfen.
