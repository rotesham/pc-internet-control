# Security / Güvenlik

This is an unsigned Windows desktop utility, provided **as is** under the MIT license. A code review and tests reduce risk; they are not a security guarantee. Read the source and warnings before running it.

- Never attach passwords, access tokens, private keys, personal logs or unredacted screenshots to issues.
- Report suspected vulnerabilities through GitHub private vulnerability reporting when available. Do not publicly disclose credentials or exploitable details before a fix.
- Default build tests use pure logic and fake OS backends. Real integration tests are explicitly opt-in and use a dedicated harmless test executable.
- EXE selection and directly selected scan roots reject reparse points (junctions/symlinks) anywhere in their ancestor chain, not just the selected file/folder. Select the real, non-linked path instead. Regression tests use disposable junctions to simulated protected folders, never Windows system files.
- Replacement is limited to ordinary, application-wide owned block rules. Service, specific-interface, address/port/protocol restrictions, allow rules, edge traversal and unsupported/unreadable extended scopes are refused before replacement writes/deletions; review them in Windows Firewall instead. Disabled ordinary rules remain restorable, including their description, exact profile mask and enabled state. Service/interface-limited rules are partial, not fully blocked.
- These checks do not provide atomic protection against a different administrator changing filesystem links or firewall rules concurrently.

Uygulama imzasızdır. Yönetici iznini yalnız güvendiğiniz sürüme verin. Güvenlik incelemesi, kusursuzluk veya mutlak güvenlik garantisi değildir.
