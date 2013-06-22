CSC=gmcs

all: hashperf.exe cryptperf.exe

hashperf.exe: hashperf.cs
	$(CSC) -sdk:4 -debug hashperf.cs

cryptperf.exe: cryptperf.cs
	$(CSC) -debug cryptperf.cs

clean:
	rm *.exe*
