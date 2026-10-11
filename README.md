# 🛡️ Rede Segura

### Think Before You Click. Stay Safe Online.

**Rede Segura** is a Windows cybersecurity application designed to help users identify potential online scams through local message analysis, contextual risk detection, and clear safety recommendations.

The project also includes an informational website where users can learn about the application and download the Windows installer.

> **Project Status:** Active Development  
> **Platform:** Windows  
> **Language:** C#, HTML, CSS, PowerShell  
> **Detection:** Local, rule-based analysis

---

## 🌐 About the Project

Online scams are becoming increasingly sophisticated. Fraudulent messages often impersonate trusted organizations, create artificial urgency, request sensitive information, or pressure victims into making unexpected payments.

Rede Segura aims to make scam awareness more accessible by helping users recognize these warning signs before taking action.

Instead of simply searching for suspicious keywords, the application evaluates several indicators and their context to produce an understandable risk assessment.

**The goal is to help people make safer decisions online — not to guarantee that every scam will be detected.**

---

## ✨ Key Features

### 🔍 Intelligent Rule-Based Message Analysis

- Analyze suspicious SMS, emails, and other text messages.
- Identify requests for passwords, PINs, and verification codes.
- Detect unexpected payment and money transfer requests.
- Recognize urgency and pressure tactics.
- Detect suspicious delivery-related messages.
- Identify potential impersonation attempts.
- Recognize suspicious prizes and reward claims.
- Examine URLs for potentially deceptive characteristics.

### 🧠 Context-Aware Risk Detection

Rede Segura goes beyond individual keywords by examining how suspicious elements appear together.

For example:

**Message A:**
"O seu código de segurança é 123456. Não o partilhe."

**Message B:**
"Envie o código de segurança que recebeu para continuar."

Although both messages mention a security code, their intentions are different.

The analyzer uses contextual rules to distinguish between warnings, ordinary information, and potentially suspicious requests.

### 💳 Payment Request Detection

The application recognizes different payment expressions and currency formats, including:

- `43$`
- `$43`
- `43 USD`
- `43€`
- `43 EUR`
- `1,95 €`

It also evaluates situations where payment is required to continue, unlock, activate, or receive something.

A payment request alone does not automatically mean a message is fraudulent.

### 🌐 Suspicious URL Analysis

The application examines URL characteristics locally, including:

- Shortened links.
- IP-based URLs.
- Potentially misleading domains.
- Unusual domain structures.
- Domains designed to resemble trusted services.

**URLs are not automatically opened or contacted during analysis.**

### 🚦 Risk Classification

Rede Segura provides four possible results:

| Risk Level | Description |
|---|---|
| 🟢 Low Apparent Risk | No significant warning signs were identified in a message with sufficient context. |
| 🟡 Moderate Risk | Potentially suspicious indicators require further verification. |
| 🔴 High Risk | Multiple or significant warning signs were detected. |
| ⚪ Inconclusive | Not enough information is available for a meaningful assessment. |

These classifications are based on heuristic rules, not verified probabilities.

### 💡 Explainable Results

The application aims to explain its assessments by presenting:

- The identified warning signs.
- Relevant parts of the message.
- An explanation of the risk.
- Practical safety recommendations.

This helps users understand **why** a message may be suspicious.

### 🔒 Privacy-Focused Analysis

- Message analysis runs locally on the Windows device.
- No external AI service is required.
- The analysis engine does not upload messages to a server.
- Suspicious URLs are not automatically visited.
- The analysis engine does not persist submitted messages.

User privacy is a core design priority.

---

## 🖥️ Windows Application

Rede Segura includes a native Windows desktop application built using **C# and Windows Forms**.

The application provides an interface for entering suspicious messages, running an analysis, and reviewing the results.

The project separates the user interface from the analysis engine, making the code easier to maintain, test, and extend.

### Windows Application Highlights

- Native desktop experience.
- Dedicated message input area.
- Risk assessment interface.
- Clear explanations and recommendations.
- Local analysis engine.
- Windows installer.

---

## 🌍 Official Website

The project includes a website developed with **HTML5 and CSS3**.

Its purpose is to:

- Introduce Rede Segura.
- Explain the application's purpose.
- Provide installation instructions.
- Explain privacy and safety considerations.
- Offer a Windows installer download.
- Provide help and support information.

**Note:** Message analysis is performed in the Windows desktop application, not on the informational website.

---

## 🛠️ Tech Stack

| Technology | Purpose |
|---|---|
| C# | Windows application and detection engine |
| Windows Forms | Native desktop user interface |
| .NET Framework | Windows runtime and application compilation |
| Regular Expressions | Text pattern detection |
| URI / IDN Processing | Local URL analysis |
| HTML5 | Website structure |
| CSS3 | Website styling and responsiveness |
| PowerShell | Build, installation, and testing automation |
| Batch | Windows utility scripts |

The current detection engine is based on **custom rules and contextual heuristics**, rather than a machine-learning model.

---

## 📁 Project Structure

```text
Rede-Segura/
│
├── index.html
├── styles.css
│
├── images/
│   └── icon.svg
│
├── rede-segura-app.cs
├── scam-analysis.cs
├── windows-installer.cs
│
├── RedeSegura.exe
├── RedeSegura-Setup.exe
│
├── build-windows-installer.ps1
├── install-windows.ps1
├── test-analyzer.ps1
├── servidor-local.ps1
├── testar-no-pc.bat
│
├── tests/
│   └── scam-analyzer-tests.cs
│
└── README.md
```

The structure shown above represents the project's documented layout. Some supporting files may need to be included when publishing the complete repository.

---

## 🚀 Installation

### Option 1 — Windows Installer

1. Download `RedeSegura-Setup.exe` from the project website or the published repository files.
2. Open the installer on a Windows computer.
3. Follow the installation instructions.
4. Launch Rede Segura from the installed shortcut.
5. Enter a message to begin analyzing potential scam indicators.

**The installer is intended for Windows devices.**

### Option 2 — Build from Source

Clone the repository:

```bash
git clone https://github.com/DavidPereiraDev/Rede-segura.git
cd Rede-segura
```

On a compatible Windows environment, run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-windows-installer.ps1
```

This script is intended to build the Windows application and installer.

Compilation requires the appropriate Windows C# compiler and supporting environment.

---

## 🧠 How the Detection Engine Works

The analysis process is divided into several stages.

### 1. Input Processing

The submitted message is normalized and prepared for analysis.

This includes handling text variations, Unicode characters, and relevant message elements while retaining references to the original text.

### 2. Signal Detection

The engine searches for indicators related to:

- Sensitive information requests.
- Urgency and threats.
- Payment demands.
- Identity claims.
- Delivery-related scams.
- Reward and prize claims.
- Suspicious URLs.

### 3. Contextual Evaluation

The engine examines combinations of indicators.

For example, a payment request associated with a condition to unlock or continue a service may receive additional attention.

Contextual evaluation also helps avoid treating every mention of money, passwords, or security as suspicious.

### 4. Weighted Risk Scoring

Different indicators contribute different weights to the assessment.

Related signals are evaluated together to reduce unnecessary double counting.

The resulting score is used alongside contextual rules to determine the risk classification.

**The score is not a statistical probability of fraud.**

### 5. Result Generation

The application returns an assessment containing the risk level, supporting evidence, and safety guidance.

---

## 🧪 Testing

The project includes a PowerShell-based testing workflow for the analysis engine.

To execute the tests on Windows:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\test-analyzer.ps1
```

The documented test suite covers scenarios such as:

- Banking scams.
- Delivery scams.
- Prize and reward scams.
- Verification code requests.
- Conditional payment requests.
- Legitimate payment notifications.
- Normal conversations.
- Educational security warnings.
- Ambiguous messages.
- Portuguese from Portugal and Brazil.

Tests also examine input handling, normalization, and extracted evidence.

**Important:** Passing manually designed tests does not establish a real-world scam detection accuracy percentage. Independent evaluation using diverse, representative messages would be necessary to measure detection performance.

---

## 🔐 Security and Privacy

Rede Segura is designed around local processing.

The analysis engine does not require transmitting submitted messages to a remote service.

The application also avoids automatically opening suspicious links during analysis.

However, users should remember that:

- Fraudulent messages can avoid known patterns.
- Legitimate messages can contain suspicious-looking language.
- A low-risk assessment is not proof that a message is safe.
- A high-risk assessment is not definitive proof of fraud.
- Sender identities are not independently verified.

When in doubt, contact the organization using an official website, app, or phone number you already trust.

**Never share passwords, authentication codes, or sensitive banking information in response to an unexpected message.**

---

## 🗺️ Roadmap

### Detection Engine

- [ ] Expand contextual analysis rules.
- [ ] Improve detection of new scam patterns.
- [ ] Reduce false positives and false negatives.
- [ ] Improve analysis of deceptive URLs.
- [ ] Evaluate the analyzer against an independent dataset.
- [ ] Explore additional languages.

### Windows Application

- [ ] Continue improving the user experience.
- [ ] Expand compatibility testing.
- [ ] Improve accessibility.
- [ ] Refine result visualization.
- [ ] Improve installation reliability.

### Website

- [ ] Improve the visual design.
- [ ] Refine responsive layouts.
- [ ] Add application screenshots.
- [ ] Improve installation documentation.

### Development

- [ ] Expand automated regression tests.
- [ ] Improve technical documentation.
- [ ] Review security and privacy practices.
- [ ] Improve build and release workflows.

---

## 🎯 Project Vision

Rede Segura was created with one mission:

**Make digital safety simpler and more accessible.**

The project aims to help everyday users recognize suspicious communication and understand the warning signs commonly associated with online scams.

By combining accessible design, local message analysis, and clear explanations, Rede Segura seeks to encourage safer decisions in an increasingly connected world.

---

## 👨‍💻 Author

**David Pereira**

Developer interested in software engineering, cybersecurity, artificial intelligence, and building practical digital products.



**GitHub:** [@DavidPereiraDev](https://github.com/DavidPereiraDev)

---

## ⚠️ Disclaimer

Rede Segura is an educational and informational cybersecurity tool.

It cannot guarantee that a message is legitimate or fraudulent and should not replace independent verification or professional cybersecurity solutions.

---

### 🛡️ Rede Segura — Think Before You Click.
