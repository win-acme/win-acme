---
sidebar: manual
---

# Upgrading
When evolving win-acme, we strive for backwards compatibility and in-place upgrades. A typical upgrade can be 
deleting everything from the program directory and extracting the new files. This can even be automated by 
tools like [Scoop](https://github.com/lukesampson/scoop).

There are some cases when you might want to be a little more careful.

- When you made changes to the script(s) included with the distributed .zip-file, you will probably want to 
  preserve those. We do accept PR's for scripts, so if they are the type of changes which others might find 
  useful too, please feel free to submit them.
- If you made changes to `wacs.exe.config`, you will probably want to preserve those. If that is the case 
  please be careful to use the newly released file as the baseline and make the necessary changes there, 
  rather than keeping the old file around. This is because the file also contains configuration information 
  for the .NET CLR which has to match the build it's been generated for.

## Testing
It's recommended to review and test all scheduled renewals after an upgrade.

## Migrations 
Some versions of win-acme have required or recommended migration steps, which are listed here. "v1.9.5" 
in this case means that you can or should read this if you're migrating from a version below 1.9.5 
to version 1.9.5 or higher. 

- [v1.9.5](/win-acme/manual/upgrading/to-v1.9.5)
- [v1.9.9](/win-acme/manual/upgrading/to-v1.9.9)
- [v2.0.0](/win-acme/manual/upgrading/to-v2.0.0)
- [v2.1.0](/win-acme/manual/upgrading/to-v2.0.0)