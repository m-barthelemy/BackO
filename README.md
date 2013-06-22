BackO
=====

cross-platform enterprise backup system.


**LAN-based backup solution, offering for now:**

- clients running on Windows (XP --> 2008R2), Linux, Solaris, FreeBSD
 
- central administration and management server

- server web GUI and command-line tool

- strong certificate authentication between clients and server, centralized certification management


**Backups support (for now)**

- integration of filesystems snapshots (VSS, LVM, Btrfs, ZFS)

- Client deduplication, fast compression, encryption

- always-full backup style for each saveset, while only performing an incremental-type (called 'Refresh) backup

- data processing (compression, encryption) can be offloaded to storage nodes

- proxy-offloaded backups of virtual machines (VMWare-VDDK)


**Backups storage**

- Any node (shared, dedicated..) can also be a storage node

- Nodes can be grouped inside a Group (by customer, or location...)

- each node inside a group can have its own 'priority' to allow to spread load amongst nodes having different physical characteristics



**Key features**

- Distributed storage (any cheap equipment with spare space can be set as a storage node)

- Source deduplication

- FS snapshots are integrated as part of the backup policies, to get better app consistency and to allow instant-restores (reverting file or whole snap if available)

- incremental-style backups seen as Full

- no central catalog/index repository

