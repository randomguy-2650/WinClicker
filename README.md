<picture align="center">
    <img
        src="./Assets/cover-image.png"
        alt=""
    />
</picture>

<h1 align="center">WinClicker</h1>

<p align="center">
    <strong>WinClicker</strong> is a high‐performance, lightweight autoclicker designed for Windows 10/11.
</p>

<details>
    <summary>🌐 Choose language</summary>
    <ul>
        <li><a href="./Translations/README.de.md">🇩🇪 Deutsch</a></li>
    </ul>
</details>

## 📦 Installation

Go to the [WinClicker Releases page](https://github.com/randomguy-2650/WinClicker/releases), scroll down to the **Assets** section and download the `.zip` file that matches your system architecture (x64 or ARM64).

Extract the contents to a folder of your choice and run the `WinClicker.exe` file inside. If you get a SmartScreen warning, click **More info** and then click **Run anyway**.

This may not work if you use [Smart App Control](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/overview) or [AppLocker](https://learn.microsoft.com/en-us/windows/security/application-security/application-control/app-control-for-business/applocker/applocker-overview), have set enterprise settings or if your antivirus flagged the program (in this case, add the extracted folder to the antivirus’s blocklist).

_Please keep the included [`LICENSE.md`](./LICENSE.md) and [`THIRD-PARTY-NOTICES.md`](./THIRD-PARTY-NOTICES.md) files in the folder with the application._

## 💻 Supported operating systems

- Windows 10, version 1809 (Build 17763) or later
- Windows 11, version 21H2 (Build 22000) or later

## 🛠️ Build from source

**Prerequisites:**

- Your computer must be running Windows 10 Build 17763, Windows 11 Build 22000 or later.
- Your computer must have Visual Studio 2022 or later installed (see step 2 if you don’t already have it).

Enable [Developer Mode](https://learn.microsoft.com/en-us/windows/advanced-settings/developer-mode) on your computer.

Download and install [Visual Studio 2026](https://visualstudio.microsoft.com/downloads) (the Community edition is sufficient).

Install the **WinUI application development** workload and check the latest Windows 11 SDK.

![Screenshot of the “Modify” window in the Visual Studio Installer](./Assets/visual-studio-modify-window.png)

Install the [XAML Styler](https://marketplace.visualstudio.com/items?itemName=TeamXavalon.XAMLStyler) extension from the Visual Studio Marketplace.

Clone the WinClicker repository with Git:

```bash
git clone https://github.com/randomguy-2650/WinClicker.git
```

Open `WinClicker.slnx` in Visual Studio to build and run WinClicker.

## 🖊️ Style guide

This project follows the [Placer Style Guide](https://github.com/placer-toolkit/placer-style-guide/?tab=readme-ov-file) for all project writing and formatting standards.

## 📄 Licence

This project is licensed under the [MIT License](./LICENSE.md).
Marketing assets are copyrighted and are not subject to the MIT License.

Dependencies are subject to their respective licences and may or may not use the MIT License used for this project.

---

© 2026 randomguy-2650 and his contributors
