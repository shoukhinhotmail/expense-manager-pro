# Privacy Policy — Expense Manager Pro

**Last updated:** August 1, 2026

Expense Manager Pro ("the App") is a Windows desktop application developed by an
independent developer. This policy explains what data the App accesses, how it's
used, and what is (and isn't) sent off your device.

## Summary

Expense Manager Pro is designed to keep your financial data on your own device.
The App does not collect, transmit, or sell any of your personal or financial
data to the developer or to any third party. The App has no analytics, no
advertising SDKs, and no tracking of any kind.

## Data Storage

All expenses, income entries, categories, budgets, and savings goals you enter
are stored locally on your device, in a SQLite database at
`%LocalAppData%\ExpenseManagerPro\`. This data never leaves your device unless
you explicitly choose to use one of the optional features below.

## Optional Features That Access the Internet

The App only makes network connections when you explicitly use one of the
following features. If you never use them, the App makes no network requests
at all.

### 1. Google Drive Backup & Restore

If you choose to back up your data, the App uses Google's OAuth sign-in to
connect to **your own** Google Drive account and uploads a copy of your local
database to a file in your Drive. Restoring works the same way in reverse.

- Only you can access this backup file — it's stored in your own Google
  account, not on any server operated by the developer.
- The developer never sees, stores, or has access to your Google credentials
  or your backed-up data. Authentication is handled entirely by Google's own
  sign-in flow.
- You can revoke the App's access at any time from your Google Account
  permissions page (myaccount.google.com/permissions).

### 2. AI Spending Insights (On-Device AI)

The AI Insights feature uses a small open-source AI model (Gemma 2, by
Google) that runs **entirely on your device** — not in the cloud.

- The first time you use this feature, the App downloads the AI model file
  (a one-time download, ~1.7 GB) from Hugging Face, a public model-hosting
  service, directly to your device.
- After that one-time download, generating insights happens completely
  offline. Your expense data is never sent anywhere to generate these
  insights — it's processed locally by the model on your own machine.
- You can delete the downloaded model at any time from within the App.

## What the Developer Never Collects

The developer does not collect, receive, or have access to:

- Your expense, income, budget, or savings data
- Your Google account credentials or Drive contents
- Usage analytics, crash reports, or telemetry of any kind
- Advertising identifiers (the App shows no ads)

## Children's Privacy

The App is a general-purpose personal finance tool and is not directed at
children. It does not knowingly collect any data from anyone, including
children, since it collects no data at all.

## Changes to This Policy

If this policy changes (for example, if a future feature adds a new optional
network connection), this page will be updated and the "Last updated" date
above will change accordingly.

## Contact

If you have questions about this privacy policy, contact:
**noushadur.rahman@gmail.com**
