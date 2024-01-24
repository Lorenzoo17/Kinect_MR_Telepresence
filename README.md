# ISTRUZIONI PER TESTARE SU HOLOLENS

## CREARE BUILD PER PIATTAFORME WINDOWS CHE FUNGA DA SERVER E COLLEGAMENTO CON HOLOLENS 2
1) Dopo aver aperto il progetto su Unity andare su File > Build Settings > Build scegliere il path ed aspettare il completamento della build.
2) Prima di avviare la build creata è necessario collegare Hololens 2 a Unity Editor.
3) Avviare l'applicazione "Holographic Remoting Player" su Hololens 2 in modo da ottenere l'indirizzo IP dell'Hololens.
4) Tornare su Unity Editor e andare su Mixed Reality > Holographic Remoting for Play Mode.
5) Nella finestra che si aprirà è necessario inserire l'indirizzo IP dell'Hololens 2 in "Remote Host Name" .
6) Inserito L'IP è possibile abilitare la Play Mode con hololens premendo su "Enable Holographic Remoting for Play Mode".

## AVVIARE ACQUISIZIONE KINECT DA HOLOLENS 2 
Se si vuole effettuare la scansione da Hololens 2 è necessario : 
1) Una volta abilitato "Enable Holographic Remoting for Play Mode" premere il tasto Play da Unity Editor, in modo da poter utilizzare l'Hololens. Apparirà una schermata di connessione con due pulsanti "Host" e "Client", per ora non premere nulla.
2) Avviare dal computer la build creata in precedenza e, nella schermata di connessione che appare, premere su "Host".
3) A questo punto da Hololens 2 è possibile premere su "Client" per collegarsi alla sessione.
4) Se si vuole avviare la scansione da Hololens è necessario premere il tasto "K" da tastiera oppure, indossando il visore, aprire il menu guardando il proprio palmo della mano e premere sul pulsante a forma di camera

## AVVIARE ACQUSIZIONE KINECT DA PC ED UTILIZZARE HOLOLENS SOLO PER VISUALIZZARE LA SCANSIONE

1) In questo caso è necessario avviare due Build
2) Avviare una prima build (BUILD 1) e non premere ancora niente
3) Avviare una seconda build (BUILD 2), che fungerà da server, quindi premere il tasto "Host"
4) Dalla prima build aperta cliccare su "Client" in modo da connettersi all'host creato
5) Avviare la Play Mode da Unity Editor in modo da poter visuallizzare la scena da Hololens 2 e premere su "Client"
6) A questo punto dalla BUILD 1 è possibile premere il tasto "K" in modo da avviare la scansione su kinect che sarà visibile da Hololens 2 
