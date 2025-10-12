<conversation_summary>
<decisions>

Indywidualne budżety uczestników są traktowane jako anonimowe sugestie dla organizatora. Ostateczny, wiążący budżet ustala organizator, biorąc pod uwagę najniższą z zasugerowanych kwot.

Zaproszenia do grupy będą realizowane za pomocą unikalnego linku do udostępnienia.

Organizator ma możliwość zdefiniowania "niejawnych reguł" losowania, polegających na wykluczeniu konkretnych par (np. partnerzy, rodzic-dziecko), aby nie mogły się wzajemnie wylosować.

Integracja z AI będzie analizować treść "listu do Mikołaja" oraz płeć (określaną na podstawie polskiego imienia) w celu generowania 3-5 propozycji prezentów w ramach ustalonego budżetu. Generowanie propozycji odbywa się na żądanie.

Kryterium sukcesu dla MVP to pomyślne przeprowadzenie procesu przez 1-2 grupy znajomych lub rodzinę.

System kont będzie oparty wyłącznie na rejestracji przez e-mail i hasło.

Powiadomienia e-mail o edycji "listu do Mikołaja" będą wysyłane z opóźnieniem (ok. 1 godziny), aby uniknąć spamu w przypadku wielokrotnych zmian.

Proces losowania jest w pełni anonimowy, również dla organizatora, który jest traktowany jak zwykły uczestnik.

W MVP nie będzie możliwości rezygnacji po losowaniu. Uczestników przed losowaniem może usuwać jedynie organizator.

Aplikacja będzie zawierać minimalne zabezpieczenia RODO w postaci prostego regulaminu i checkboxa ze zgodą na przetwarzanie danych podczas rejestracji.

"List do Mikołaja" jest opcjonalnym, pojedynczym polem tekstowym i może być edytowany przez użytkownika w dowolnym momencie, również po przeprowadzeniu losowania.

Funkcjonalność anonimowego czatu między uczestnikami zostaje przeniesiona do backlogu i nie będzie realizowana w MVP.

</decisions>

<matched_recommendations>

Ustalanie budżetu: Organizator ustala jeden, obowiązujący wszystkich budżet, a anonimowa, posortowana lista sugestii uczestników pomaga mu podjąć decyzję komfortową dla wszystkich.

System zaproszeń: Wdrożenie generowania unikalnego linku jest rozwiązaniem prostszym technicznie i dającym organizatorowi elastyczność.

Walidacja reguł: System powinien w czasie rzeczywistym walidować zdefiniowane przez organizatora reguły wykluczeń i informować o ewentualnych konfliktach uniemożliwiających losowanie.

Źródło informacji: Aplikacja internetowa jest głównym i wiarygodnym źródłem informacji o wyniku losowania; powiadomienia e-mail pełnią rolę pomocniczą.

Dyskretne powiadomienia: E-mail o aktualizacji "listu do Mikołaja" powinien być zwięzły i zachęcać do zalogowania w aplikacji, bez ujawniania nowej treści.

Jednorazowość losowania: Akcja "Uruchom losowanie" jest nieodwracalna, a jej interfejs powinien jasno to komunikować

Generowanie sugestii AI: Sugestie AI powinny być generowane "na żądanie" po stronie kupującego, co oszczędza zasoby i zapewnia ich aktualność.

Zakończenie procesu w MVP: Podróż użytkownika w MVP kończy się w momencie zapoznania się z wynikiem losowania. Dodatkowe statusy ("prezent kupiony") nie są implementowane.
</matched_recommendations>

<prd_planning_summary>

Główne wymagania funkcjonalne produktu
System Kont Użytkowników:

Rejestracja i logowanie wyłącznie za pomocą adresu e-mail i hasła.

Prosty profil użytkownika zawierający imię, nazwisko oraz opcjonalny "list do Mikołaja".

Podczas dołączania do grupy użytkownik podaje anonimową sugestię budżetową.

Panel Organizatora:

Tworzenie grupy i generowanie unikalnego linku zaproszeniowego.

Widok listy uczestników z możliwością ich ręcznego usuwania przed losowaniem.

Dostęp do anonimowej, posortowanej listy sugestii budżetowych od uczestników.

Narzędzie do definiowania reguł wykluczeń (par), które uniemożliwiają wzajemne wylosowanie się uczestników.

Możliwość ustalenia ostatecznego, wiążącego budżetu dla całej grupy.

Przycisk "Uruchom losowanie", który jest akcją nieodwracalną.

Proces Losowania i Powiadomienia:

Algorytm losujący, który uwzględnia zdefiniowane reguły wykluczeń.

Automatyczne powiadomienia e-mail do wszystkich uczestników z informacją o zakończonym losowaniu i zachętą do sprawdzenia wyników w aplikacji.

Powiadomienia e-mail o aktualizacji "listu do Mikołaja" przez osobę obdarowywaną, wysyłane z godzinnym opóźnieniem.

Panel Uczestnika:

Widok statusu przed losowaniem ("Oczekiwanie na start...").

Po losowaniu, jasna informacja o tym, dla kogo dany użytkownik przygotowuje prezent (imię i nazwisko).

Dostęp do "listu do Mikołaja" osoby obdarowywanej.

Możliwość edycji własnego "listu do Mikołaja" w dowolnym momencie.

Przycisk do generowania na żądanie propozycji prezentów od AI, na podstawie "listu", płci i budżetu.

Kluczowe historie użytkownika i ścieżki korzystania
Ścieżka Organizatora:

Jako organizator, chcę stworzyć grupę, aby móc zarządzać losowaniem prezentów.

Chcę otrzymać unikalny link, aby łatwo zaprosić znajomych i rodzinę.

Chcę widzieć anonimowe sugestie budżetowe, aby ustalić kwotę, która nikogo nie obciąży.

Chcę móc zdefiniować, że np. mąż i żona nie mogą się wzajemnie wylosować, aby zabawa była ciekawsza.

Chcę w jednym momencie uruchomić losowanie dla wszystkich i również wziąć w nim udział jako uczestnik.

Ścieżka Uczestnika ('Mikołaja'):

Jako uczestnik, chcę dołączyć do grupy za pomocą linku, podając swoje imię i sugerowany budżet.

Chcę stworzyć swój "list do Mikołaja", aby ułatwić zadanie osobie, która mnie wylosuje.

Chcę otrzymać powiadomienie, gdy losowanie się odbędzie, i po zalogowaniu dowiedzieć się, dla kogo kupuję prezent.

Chcę mieć możliwość wygenerowania pomysłów na prezent, jeśli "list do Mikołaja" jest niejasny lub pusty.

Chcę móc zaktualizować swój "list", jeśli zmienię zdanie, nawet po losowaniu.

Ważne kryteria sukcesu i sposoby ich mierzenia
Główne kryterium: Pomyślne przeprowadzenie co najmniej 1-2 pełnych cykli losowania w zamkniętych grupach (rodzina, znajomi).

Sposób pomiaru: Obserwacja i zebranie bezpośredniego feedbacku od uczestników tych grup. Sukcesem jest sytuacja, w której proces od założenia grupy do poznania wyników przebiegł bez błędów technicznych i był zrozumiały dla wszystkich użytkowników.

</prd_planning_summary>

<unresolved_issues>

Mechanizm powiadomień po losowaniu: Zdecydowano, że "list do Mikołaja" można edytować w dowolnym momencie, a powiadomienia o zmianach mają godzinne opóźnienie. Należy potwierdzić, czy ta zasada opóźnienia ma obowiązywać również po losowaniu. Częste zmiany mogą prowadzić do irytujących powiadomień dla kupującego, który być może już rozpoczął poszukiwania prezentu na podstawie wcześniejszej wersji listy. Należy rozważyć, czy po losowaniu powiadomienia nie powinny być np. zbiorcze (raz dziennie) lub opcjonalne.
</unresolved_issues>
</conversation_summary>