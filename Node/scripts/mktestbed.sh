mkdir testbed
cd testbed
x=0; while [  -lt 1000 ]; do mkdir ; y=0; while [  -lt 1000 ]; do touch /; y=1; done; x=1; done
mkdir specialitems
touch specialitems/file_with_xattrs
setfattr -n user.test1 -v test xattr 1 specialitems/file_with_xattrs
