namespace Factoriod.Models.Game.Test;

public class MapExchangeStringTests
{
    [Fact]
    public void DefaultSettings()
    {
        var mapExchangeString = @">>>eNpjZGBkCGAAgwZ7EOZgSc5PzIHxQJgrOb+gILVIN78oFVmYM7mo
NCVVNz8TVXFqXmpupW5SYjGKYo7Movw8dBNYi0vy81BFSopSU4uRRbh
LixLzMktz0fUyMN7VdFrU0CLHAML/6xkU/v8HYSDrAdAvIMzA2ABRCR
SDAdbknMy0NAYGBUcgdgJJMzIyVousc39YNcWeEaJGzwHK+AAVOZAEE
/GEMfwccEqpwBgmSOYYg8FnJAbE0hKgFVBVHA4IBkSyBSTJyNj7duuC
78cu2DH+Wfnxkm9Sgj2joavIuw9G6+yAkuwgfzLBiVkzQWAnzCsMMDM
f2EOlbtoznj0DAm/sGVlBOkRAhIMFkDjgzczAKMAHZC3oARIKMgwwp9
nBjBFxYEwDg28wnzyGMS7bo/sDGBA2IMPlQMQJEAG2EO4yRgjTod+B0
UEeJiuJUALUb8SA7IYUhA9Pwqw9jGQ/mkMwIwLZH2giKg5YooELZGEK
nHjBDHcNMDwvsMN4DvMdGJlBDJCqL0AxCA8kAzMKQgs4gIObmQEBgGk
jdtPMKQAvV6Gx<<<";

        MapExchangeString.Parse(mapExchangeString);
    }

    [Fact]
    public void NonDefaultSettings()
    {
        var mapExchangeString =@">>>eNpjZGBkCGAAAweH1atW2XOwJOcn5gBZdgwMDXarV2nZcSXnF
xSkFunmF6WCVDEwHLAHKeRMLipNSdXNz8wBCtoDFYNFuVLzUnMrd
ZMSi1NBQjDMkVmUnwcx4QDQhAYHkHWsxSX5eVBlIBMY7FlLilJTi
5E1cpcWJeZlluZC9IIcBHIYgz0D46214skNLXIMIPy/nkHh/38QB
rIeAOVBmIGxAWQLAyNQDAZYk3My09IYGBQcgdgJZAEjI2O1yDr3h
1VT7BkhavQcoIwPUJEDSTARTxjDzwGnlAqMYYJkjjEYfEZiQCwtA
VoBVcXhgGBAJFtAkoyMvW+3Lvh+7IId45+VHy/5JiXYMxq6irz7Y
LTODijJDvInE5yYNRMEdsK8wgAz84E9VOqmPePZMyDwxp6RFaRDB
EQ4WACJA97MDIwCfEDWgh4goSDDAHOaHcwYEQfGNDD4BvPJYxjjs
j26P4ABYQMyXA5EnAARYAvhLmOEMB36HRgd5GGykgglQP1GDMhuS
EH48CTM2sNI9qM5BDMikP2BJqLigCUauEAWpsCJF8xw1wDD8wI7j
Ocw34GRGcQAqfoCFIPwIJkFYhSEFnAABzczAwIA08bkrGI1AGLpp
10=<<<";

        MapExchangeString.Parse(mapExchangeString);
    }
}
