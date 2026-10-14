# NEXUS OPTIMIZER

Application Windows WPF en C#/.NET 8, interface sombre violette, tableau de bord de performances, gestion de processus, nettoyage temporaire avec confirmation, raccourcis vers les réglages Windows et profil d'alimentation.

## Important : état de livraison

Ce dépôt contient le projet source et le pipeline de compilation Windows. **Le fichier `NEXUS_Setup.exe` n'est pas inclus ni prétendu compilé** depuis cet environnement. Il est généré par le workflow Windows décrit ci-dessous.

## Créer le véritable installateur Windows

1. Crée un dépôt GitHub et importe le contenu de ce dossier.
2. Dans GitHub, ouvre l'onglet **Actions**.
3. Sélectionne **Build NEXUS Windows installer**.
4. Clique **Run workflow**.
5. Attends la fin du job (vert).
6. Ouvre le run terminé et télécharge l'artefact **NEXUS_Setup**.
7. Dans l'archive téléchargée, récupère `NEXUS_Setup.exe` et double-clique dessus.

Le workflow compile l'application en autonome (`--self-contained true`) et crée un installateur Inno Setup avec raccourcis et désinstallation. Aucune installation de Python n'est requise sur le PC cible.

## Compilation locale (Windows)

Pré-requis : .NET 8 SDK et Inno Setup 6.
```powershell
dotnet publish src/NexusOptimizer/NexusOptimizer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
New-Item -ItemType Directory -Force artifacts
& "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" installer/NEXUS.iss
```

## Fonctionnalités actuelles

- Dashboard CPU, estimation RAM et stockage C:.
- Profils d'alimentation Haute performance / Équilibré avec confirmation.
- Accès aux paramètres Mode Jeu et applications de démarrage Windows.
- Liste des 30 processus les plus gourmands en mémoire, fermeture avec confirmation et protection basique de processus système.
- Analyse et nettoyage des fichiers temporaires utilisateur, en ignorant les fichiers verrouillés.
- Journal en mémoire des actions pendant la session.

## Limites et sécurité

- Les relevés CPU/RAM dépendent des API Windows et peuvent être partiels. La RAM affichée est une estimation fondée sur les processus accessibles, pas une mesure parfaite de la mémoire physique.
- La température GPU/CPU n'est pas affichée : les interfaces Windows génériques ne l'exposent pas de manière fiable sur tous les PC.
- Le nettoyage de fichiers temporaires ne supprime que les fichiers à la racine du dossier temporaire utilisateur, et ignore les fichiers occupés.
- Les réglages d'alimentation peuvent être indisponibles sur certains PC.
- Le logiciel ne désactive pas Defender, le pare-feu, Windows Update ni des services système. Il ne garantit aucun gain de FPS.
- Le journal est conservé uniquement en mémoire et est réinitialisé à la fermeture.
- Teste le logiciel sur une machine non critique avant usage courant.

## Structure

- `src/NexusOptimizer/` : application WPF.
- `installer/NEXUS.iss` : script d'installation Inno Setup.
- `.github/workflows/build-windows.yml` : compilation automatique sur Windows.
