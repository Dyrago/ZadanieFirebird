Firebird Metadata Tool (DbMetaTool)

Aplikacja konsolowa napisana w .NET 8.0, służąca do zarządzania metadanymi bazy danych Firebird 5.0. Narzędzie umożliwia eksport struktury bazy do skryptów SQL, budowanie nowej bazy na ich podstawie oraz aktualizację istniejącej bazy.

🚀 Funkcjonalności

Aplikacja realizuje trzy główne operacje:

    Build Database (build-db): Tworzy nowy plik bazy danych (.fdb) w pustym katalogu i zasila go strukturą ze wskazanych skryptów SQL.
  # Ważne! #
  
  Nazwy plików muszą umożliwić aplikacji wykonanie skryptów W kolejności Domeny -> Tabele -> Procedury 
  Należy umożliwić to poprzez numerację jak poniżej, lub nazwy, sortowanie w aplikacji odbywa się po nazwach plików.

    Export Scripts (export-scripts): Łączy się z istniejącą bazą i generuje skrypty SQL dla:

        Domen (001_domains.sql)

        Tabel (002_tables.sql)

        Procedur (003_procedures.sql)

    Update Database (update-db): Wykonuje skrypty SQL na istniejącej bazie danych, bezpiecznie parsując pliki (obsługa terminatorów SET TERM dla procedur).

Przy eksporcie każdy rodzaj meatadanych jest umieszczany w jednym pliku odpowiadącym jego typowi.

🛠️ Wymagania

Aby uruchomić aplikację, potrzebujesz:

    .NET 8.0 SDK (do kompilacji i uruchomienia).

    Serwer Firebird 5.0 (zainstalowany i uruchomiony).

    Biblioteka kliencka fbclient.dll (zazwyczaj dostarczana z instalacją Firebird lub pakietem NuGet).

⚙️ Instalacja i Kompilacja

    Sklonuj repozytorium:

    git clone https://github.com/Dyrago/ZadanieFirebird.git
    cd ZadanieFirebird

Zbuduj projekt:

    dotnet build
    
    lub
    
    dotnet publish

📖 Sposób użycia

Aplikacja działa w trybie CLI (Command Line Interface). Poniżej znajdują się przykłady użycia poszczególnych poleceń.

1. Budowanie nowej bazy danych

Tworzy nową bazę w podanym katalogu i wykonuje na niej skrypty.

dotnet run -- build-db --db-dir "C:\Dane\NowaBaza" --scripts-dir "C:\Dane\Skrypty"

    --db-dir: Katalog, w którym zostanie utworzony plik database.fdb.

    --scripts-dir: Katalog zawierający pliki .sql.

2. Eksportowanie skryptów (Metadane)

Pobiera strukturę istniejącej bazy i zapisuje ją do plików SQL.


    dotnet run -- export-scripts --connection-string "User=SYSDBA;Password=masterkey;Database=localhost:C:\Dane\MojaBaza.fdb;DataSource=localhost;Port=3050;Dialect=3;Charset=UTF8;" --output-dir "C:\Dane\Eksport"

    --connection-string: Pełny ciąg połączeniowy do bazy Firebird.

    --output-dir: Katalog, w którym zostaną zapisane pliki 001_domains.sql, 002_tables.sql, 003_procedures.sql.

3. Aktualizacja bazy danych

Uruchamia skrypty SQL na istniejącej bazie danych.

    dotnet run update-db --connection-string "User=SYSDBA;Password=masterkey;Database=local" --scripts-dir "C:\Dane\Aktualizacje"

# Dodatkowe: #

W wypadku problemów z uruchomieniem poprzez dotnet run <komenda-cli>
Po zbudowaniu przechodzimy do bin -> Debug lub Release -> net8.0 i z tego poziomu uruchamiamy 
CMD lub Powershell jako administrator. Następnie wywołujemy aplikację poprzez

    ./DbMetaTool.exe <komenda-cli>
