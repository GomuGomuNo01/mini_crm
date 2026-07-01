# Mini-CRM — Gestion clients & contrats

Application web **ASP.NET Core MVC (.NET 10)** de gestion de clients et de leurs
contrats, avec authentification par rôles, tableau de bord, audit automatique des
actions et une interface moderne pilotée en AJAX (modals, panneaux latéraux,
recherche dynamique).

---

## Sommaire

- [Fonctionnalités](#fonctionnalités)
- [Stack technique](#stack-technique)
- [Architecture du projet](#architecture-du-projet)
- [Prérequis](#prérequis)
- [Installation pas à pas](#installation-pas-à-pas)
- [Identifiants de démonstration](#identifiants-de-démonstration)
- [Rôles & permissions](#rôles--permissions)
- [Journal d'audit](#journal-daudit)
- [Sécurité](#sécurité)
- [Exposer l'application avec ngrok](#exposer-lapplication-avec-ngrok)
- [Tests](#tests)
- [Commandes utiles](#commandes-utiles)
- [Dépannage](#dépannage)

---

## Fonctionnalités

### Gestion commerciale
- **Clients** : CRUD complet (nom, email, secteur, statut), recherche dynamique,
  pagination.
- **Contrats** : CRUD complet, liés à un client, montant, dates de début/fin,
  statut, badge **« Expire bientôt »** (contrats actifs expirant sous 30 jours),
  recherche + filtre par statut, pagination, **export CSV** protégé contre
  l'injection de formule.
- **Secteurs** : catalogue d'activités géré par l'administrateur (ajout,
  renommage avec propagation aux clients, suppression), proposé comme liste
  déroulante dans le formulaire client avec option « Autre » pour en créer un
  nouveau à la volée.
- **Dashboard** : KPIs (clients actifs, contrats actifs, contrats expirant,
  valeur totale), graphique en donut (Chart.js) de répartition des contrats par
  statut, liste des prochaines expirations.

### Administration
- **Utilisateurs** : création et suppression de comptes par un administrateur
  (rôles Admin / Utilisateur) — pas d'inscription publique.
- **Journal d'audit** : toute action qui modifie la base de données (création,
  modification, suppression d'un client, contrat, secteur ou utilisateur) est
  enregistrée **automatiquement**, avec l'auteur, la date/heure et le détail
  champ par champ des changements (« avant → après »).

### Expérience utilisateur
- Interface **responsive** (sidebar rétractable, mise en page adaptée mobile).
- CRUD sans rechargement de page : **modals centrés** (création, suppression,
  non fermables au clic extérieur) et **panneaux latéraux** glissants
  (consultation en lecture seule, édition).
- **Notifications toast** de confirmation après chaque action.
- **Filtres de recherche dynamiques** (déclenchés à la saisie / au changement de
  sélection, sans rechargement).
- Champs de mot de passe avec bouton afficher/masquer.
- Verrouillage de compte après plusieurs échecs de connexion.

---

## Stack technique

| Couche           | Technologie                                    | Version   |
|-------------------|------------------------------------------------|-----------|
| Framework web     | ASP.NET Core MVC                                | .NET 10   |
| ORM               | Entity Framework Core                           | 10.x      |
| Base de données   | MySQL (via WAMP en local)                       | 8.x / 9.x |
| Provider EF       | `MySql.EntityFrameworkCore` (Oracle officiel)   | 10.0.7    |
| Authentification  | ASP.NET Core Identity                           | inclus    |
| Front-end         | Bootstrap 5 + Bootstrap Icons                   | 5.3       |
| Graphiques        | Chart.js (CDN)                                  | 4.x       |
| Tunnel de démo    | ngrok                                            | v3        |

> **Pourquoi MySQL et pas SQL Server ?** Le projet est pensé pour tourner en
> local avec WAMP. Le provider `MySql.EntityFrameworkCore` d'Oracle est utilisé
> car c'est actuellement le seul compatible avec EF Core 10 (Pomelo, l'autre
> provider MySQL courant, s'arrête à EF Core 9 au moment de l'écriture).

---

## Architecture du projet

```
MiniCrm/
├── Areas/Identity/Pages/Account/   Pages Identity personnalisées (Login ; Register désactivé)
├── Controllers/                    Client, Contract, Dashboard, Sector, User, Audit, Home
├── Models/                         Client, Contract, Sector, ApplicationUser, AuditLog
├── ViewModels/                     PagedResult<T>, DashboardViewModel, CreateUserViewModel, ...
├── Services/                       Pattern Interface + Service (logique métier hors des controllers)
│   ├── ClientService / IClientService
│   ├── ContractService / IContractService
│   ├── SectorService / ISectorService
│   └── AuditService / IAuditService
├── Data/
│   ├── ApplicationDbContext.cs     DbContext (Identity + entités métier)
│   ├── AuditSaveChangesInterceptor.cs   Intercepteur EF Core : journalise chaque SaveChanges
│   └── SeedData.cs                 Rôles, admin de démo, données d'exemple
├── Views/
│   ├── Client/ Contract/ Sector/ User/ Dashboard/ Audit/  Vues + partials AJAX (_XxxForm, _XxxTable, _XxxDetails)
│   └── Shared/_Layout.cshtml       Layout (sidebar, modal CRUD, offcanvas, toasts)
├── Migrations/                     Historique des migrations EF Core
├── wwwroot/                        CSS (design system), JS (interactions AJAX), librairies front
├── ngrok.yml                       Config de tunnel ngrok (non versionné, contient un token)
└── Program.cs                      Pipeline, DI, Identity, sécurité, seed au démarrage
```

Les **controllers restent volontairement fins** : toute la logique métier
(recherche, pagination, création du secteur à la volée, etc.) vit dans les
services, ce qui facilite les tests et la maintenance.

---

## Prérequis

| Outil | Usage | Lien |
|---|---|---|
| **.NET 10 SDK** | Compiler et exécuter le projet | https://dotnet.microsoft.com/download |
| **WAMP** (ou tout serveur MySQL 8+) | Héberger la base `mini_crm` | https://www.wampserver.com/ |
| **Outil EF Core CLI** | Générer/appliquer les migrations | `dotnet tool install --global dotnet-ef` |
| **ngrok** *(optionnel)* | Exposer l'application sur Internet pour une démo | https://ngrok.com/download |
| Un IDE .NET (Rider, Visual Studio, VS Code) | Confort de développement | — |

Vérifier les installations :

```powershell
dotnet --version     # doit afficher 10.x
dotnet ef --version  # doit afficher 10.x (après installation de l'outil)
```

---

## Installation pas à pas

### 1. Cloner le projet

```powershell
git clone <url-du-repo>
cd MiniCrm
```

### 2. Démarrer MySQL (WAMP)

1. Lancer WAMP et s'assurer que le service MySQL est démarré.
2. Créer une base nommée **`mini_crm`** (via phpMyAdmin ou la ligne de commande) :
   ```sql
   CREATE DATABASE mini_crm CHARACTER SET utf8mb4;
   ```
3. **Important — moteur de stockage** : MySQL doit utiliser **InnoDB** par
   défaut (obligatoire pour les tables Identity, qui dépassent la limite
   d'index de MyISAM). Dans le `my.ini` de votre instance MySQL (section
   `[wampmysqld64]` ou `[mysqld]`) :
   ```ini
   default_storage_engine=InnoDB
   ```
   Redémarrer le service MySQL après modification.

### 3. Configurer la chaîne de connexion

Dans [`appsettings.json`](appsettings.json), adapter si besoin utilisateur/mot
de passe (par défaut : `root` sans mot de passe, standard WAMP) :

```json
"ConnectionStrings" : {
  "DefaultConnection": "server=localhost;port=3306;database=mini_crm;user=root;password="
}
```

### 4. Installer les outils et restaurer les paquets

```powershell
dotnet tool install --global dotnet-ef
dotnet restore
```

### 5. Appliquer les migrations

Crée toutes les tables (Identity + Client, Contract, Sector, AuditLog) dans
`mini_crm` :

```powershell
dotnet ef database update
```

### 6. Lancer l'application

```powershell
dotnet run
```

Au premier démarrage, [`SeedData`](Data/SeedData.cs) initialise automatiquement :
- les rôles **Admin** et **User** ;
- un compte administrateur de démonstration ;
- un compte utilisateur de démonstration ;
- 10 clients et 20 contrats d'exemple ;
- le catalogue de secteurs (déduit des clients d'exemple).

L'application est accessible sur **http://localhost:5154**.

---

## Identifiants de démonstration

| Rôle  | Email                | Mot de passe |
|-------|-----------------------|--------------|
| Admin | `admin@minicrm.dev`  | `Admin@1234` |
| User  | `user@minicrm.dev`   | `User@1234`  |

> Ces identifiants ne sont affichés dans l'écran de connexion qu'en
> environnement **Development** — ils disparaissent automatiquement en
> production.

---

## Rôles & permissions

| Action | Admin | User |
|---|---|---|
| Consulter clients / contrats / dashboard | ✅ | ✅ |
| Créer / modifier / supprimer un client ou contrat | ✅ | ❌ (pop-up d'accès refusé) |
| Gérer les secteurs | ✅ | ❌ |
| Créer / supprimer des utilisateurs | ✅ | ❌ |
| Consulter le journal d'audit | ✅ | ❌ |

Toutes les routes sont protégées par défaut (`FallbackPolicy` exigeant une
authentification) ; les actions sensibles sont en plus explicitement
restreintes avec `[Authorize(Roles = "Admin")]`.

---

## Journal d'audit

Le journal d'audit ne dépend **pas** d'appels manuels dans les controllers : un
[`AuditSaveChangesInterceptor`](Data/AuditSaveChangesInterceptor.cs) s'accroche
à chaque `SaveChanges` d'Entity Framework et détecte automatiquement toute
création, modification ou suppression d'un `Client`, `Contract` ou `Sector` (et
les créations/suppressions d'utilisateurs sont journalisées explicitement dans
`UserController`). Ainsi, **toute nouvelle fonctionnalité qui modifie ces
entités est tracée sans code supplémentaire**.

Pour chaque modification, le détail liste chaque champ changé sur sa propre
ligne :

```
Nom : « client_test » → « client_test_mod »
Secteur : « Automobile » → « Bâtiment »
Statut : « Prospect » → « Active »
```

Le journal est accessible via **Administration → Journal d'audit** (Admin
uniquement), avec pagination.

---

## Sécurité

- **Anti-force brute** : verrouillage de compte 15 minutes après 5 tentatives
  de connexion échouées.
- **CSRF** : jeton anti-forgery validé sur tous les formulaires POST.
- **Injection SQL** : impossible via l'ORM (Entity Framework paramètre toutes
  les requêtes) ; aucune requête SQL brute dans le code.
- **Injection de formule CSV** (CWE-1236) : les cellules de l'export CSV
  commençant par `= + - @` sont neutralisées avant export.
- **En-têtes HTTP de sécurité** : `Content-Security-Policy`,
  `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`,
  `Referrer-Policy`.
- **Reverse proxy (ngrok)** : `ForwardedHeaders` configuré pour respecter le
  schéma HTTPS transmis par le tunnel.
- **Mots de passe** hachés par ASP.NET Core Identity (jamais stockés en clair).

---

## Exposer l'application avec ngrok

Un fichier [`ngrok.yml`](ngrok.yml) (non versionné — il contient un jeton
d'authentification personnel) définit un tunnel HTTP vers le port `5154` de
l'application.

1. Récupérer son propre *authtoken* sur https://dashboard.ngrok.com et
   l'insérer dans `ngrok.yml` :
   ```yaml
   version: "3"
   agent:
     authtoken: <votre-token>
   tunnels:
     minicrm:
       proto: http
       addr: 5154
   ```
2. Démarrer l'application (`dotnet run`) puis, dans un autre terminal :
   ```powershell
   ngrok start minicrm --config ngrok.yml
   ```
3. ngrok affiche une URL publique (`https://xxxx.ngrok-free.app`) redirigeant
   vers l'application locale — pratique pour une démonstration à distance.

---

## Tests

Le projet [`MiniCrm.Tests`](MiniCrm.Tests/) contient une suite de **52 tests
unitaires** (xUnit) couvrant la logique métier :

| Fichier | Ce qui est testé |
|---|---|
| `Services/ClientServiceTests.cs` | CRUD client, recherche, pagination, création automatique d'un secteur à partir du formulaire |
| `Services/ContractServiceTests.cs` | CRUD contrat, filtre par statut et recherche, export |
| `Services/SectorServiceTests.cs` | Catalogue de secteurs, doublons, renommage avec propagation aux clients |
| `Services/AuditServiceTests.cs` | Écriture des entrées d'audit, troncature, pagination |
| `Data/AuditSaveChangesInterceptorTests.cs` | **Audit automatique** : création/modification/suppression détectées sans appel manuel, détails multi-lignes, absence d'audit hors authentification, pas de boucle infinie |
| `Models/ContractTests.cs` | Calcul de `IsExpiringSoon` (bornes de la fenêtre des 30 jours) |

Les tests utilisent le provider **EF Core InMemory** (aucune dépendance à
MySQL/WAMP n'est nécessaire pour les exécuter).

```powershell
# Lancer toute la suite de tests
dotnet test MiniCrm.Tests/MiniCrm.Tests.csproj

# Lancer un seul fichier / une seule classe de tests
dotnet test MiniCrm.Tests/MiniCrm.Tests.csproj --filter "FullyQualifiedName~ClientServiceTests"
```

> Le projet de tests vit dans un sous-dossier de la solution ; le `.csproj`
> principal exclut explicitement ce dossier de son *globbing* par défaut pour
> éviter tout conflit de compilation.

### Résultat de la dernière exécution

```
Série de tests pour D:\PROJET PERSO\MiniCrm\MiniCrm.Tests\bin\Debug\net10.0\MiniCrm.Tests.dll (.NETCoreApp,Version=v10.0)
Au total, 1 fichiers de test ont correspondu au modèle spécifié.

Réussi!  - échec :     0, réussite :    52, ignorée(s) :     0, total :    52, durée : 1 s - MiniCrm.Tests.dll (net10.0)
```

---

## Commandes utiles

```powershell
# Lancer l'application en mode développement
dotnet run

# Créer une nouvelle migration après modification d'un modèle
dotnet ef migrations add NomDeLaMigration

# Appliquer les migrations en attente
dotnet ef database update

# Revenir à une migration précédente
dotnet ef database update NomMigrationPrecedente

# Compiler sans lancer
dotnet build
```

---

## Dépannage

| Symptôme | Cause probable | Solution |
|---|---|---|
| `La clé est trop longue. Longueur maximale : 1000` lors de `database update` | MySQL utilise MyISAM par défaut | Basculer `default_storage_engine=InnoDB` dans `my.ini` et redémarrer MySQL |
| Page de login sans CSS | Fichiers statiques bloqués par l'authentification | Déjà corrigé (`MapStaticAssets().AllowAnonymous()`) — vérifier que le build est à jour |
| `dotnet run` échoue avec un fichier `.exe` verrouillé | Une instance précédente tourne encore | Fermer le terminal qui exécute `dotnet run` (Ctrl+C) avant de relancer |
| Connexion à MySQL refusée | Service MySQL arrêté ou mauvais identifiants | Vérifier que WAMP est démarré et que `appsettings.json` correspond à votre configuration |

---

**Auteur** : Cédric Kouadio — projet réalisé dans le cadre d'un entretien
technique (Sokhar).
