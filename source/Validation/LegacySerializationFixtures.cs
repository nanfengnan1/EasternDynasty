using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

// Frozen historical inputs, not files generated during validation. The exact bytes
// were captured from Validation/serialization-{state}.json, written by
// EasternDynasty.Editor.ProjectTools.VerifySerialization (Unity JsonUtility),
// with source file modification time 2026-10-25 11:39:24 Asia/Shanghai.
// Keep these v1 saves immutable: regenerating them from today's World would weaken
// backward-compatibility coverage. SHA256 verifies every unpacked source payload.
// GZip/Base64 keeps all four 972-tile maps in a snapshot-included .cs test source.
// CLR deserialization of these fixtures does not replace publisher JsonUtility tests.
internal static class LegacySerializationFixtures
{
    internal static Stream Open(string state)
    {
        string encoded;
        string expectedSha;
        switch (state)
        {
            case "idle":
                expectedSha = "6F9D10CD8B5D888FF7585153BFF3412904DC07E73A38FD88DEC00DAE0DF20B3E";
                encoded =
                    "H4sIAAAAAAACCs2dXW9lxZmF/0p0rq1o13eVb3OT3I2SycwFQpHTPt14MHbLdjMoCAkYQLQCiMyEJGhadOYDQZTw0fQwIT3M5MekT9t9lb8wOh/Q7xK1YVfi" +
                    "t9a5idrBdq9e9dZbu9ap2s+zs9O9p+d/Nz85PTg+mu2andlTe9e/tz/bnc1Wf/z7g/2zJ2a7w+qL784Prj1x9sVX358/fbD+qWFndnLjcH4y2539YH5ysHd4" +
                    "8JO9s4Pj2c7s+OTg2sH6O358cHL2xPfn11Y/4XdmTxw/NV/+RS6FndnZyfHx9dPZrg/Dzuxo/szZd/ZO9pf/1e/M9g4P/+b4+PD0h0eHx1eenO/Pdq/uHZ7O" +
                    "d2bzw73rp8uv3TB8e9hZ/Uv2f3h25W8Prjx5ulY1Pz2+cXJlfjrbfXZ27WTv9HT5zXb13ddO9pbK/ODWP3x2fDSf7ZpswvLLg5OlzDKkb4ed2bXjw/3Zrolm" +
                    "9Z3/sLc/X/2dz+3MDveO9k9nu489OzvYX/2Nz6z+9yebv/3aF/Yczp+eH852w/qfvfyp1f/95MHyD25ndvyPR+IfdmXv+sHZ3uGXXy9/5ssvfnxysH/t0ZdH" +
                    "e8v/Nnv4Ly8sbt2bPbezVmJWSkyrkuESlJx/8uH5zz/+UoldKbHjSmwvJW6lxI0r8b2U+JUSP67EVJXYy1By+66sk7BSErZgdOJKSWz1REFJWilJW+BJXinJ" +
                    "W+BJWSkp40pcVYm5BCUXr9+EzrZusmbYgt5mNm3WbEGtmHWjNV/ptOYbfFHoKmbdao0b19Kt15p1szV+XEu/MVq3WxNax0hDy7rhmjiupVt3MeuWa9I2jNG6" +
                    "6Zq8Db6s264p41rqfddfgpYHn9x98OkLj57j1n3XDlvQX+y671rTOkYK65HdPOA2912Fp3677rvWtc4jDS3rvmu/0nftN9Suxh5k3XdtGNfiu9Xuuu/aOK6l" +
                    "395s3Xdt2gYt675r87iWbn3XrvuuLeNa+u1a133XDVvgi1v3XWe2YE67dd91dgtq122iBdfqi0Lfdeu+6/w21Mu677rQ6ovCOu2iCOcMN4hKIp0z03fTCquR" +
                    "yyKeM9M30xoDVEQ+x5XiBxHQcWvFG5HQGWoG5K2I6MiuOJHRkV3xIqQjuxJESkeWEmVKx+1xPsmUjlwtWaZ0ZvpuUaPLFZnSGWoaFQaZ0pnpSYeGFiNTOkNN" +
                    "gIKVKZ2hJmPByZSO7IuXKZ2ZnjArJGMhyJSO7EuUKV2DFo3PFpNM6RrmtEKvC1mmdOT+UmRK16AlXIKWxZ07i1viU9dBpnSGuqOPRqZ0ZnrqouGLlSmdoe6i" +
                    "o5MpHVmLlymdoSZjMciUzkxPdTXqJcqUjuxLkikduV6yTOka+ovCehSLTOkafFHQkgaZ0nH7bjIypTPUJDVZkdLZ6VtYjSFyIqWz1B1s8iKls9OPrinEyymI" +
                    "lM5SM4YURUpnuUfGkkjpyK5kkdKRXSkipeO6kgeR0nFdyUakdGRXrEzpLPVcX3YypeO2/uxlSmepyVgOMqWz03eLGr5EmdJZatKRk0zpLDUZy1mmdGRfikzp" +
                    "uL6UQaZ0DVoUUrpiZEpH9sXKlI5bL8XJlK7BF4X+UrxM6bi9rgSZ0nHXgBJlSmen79AU1umSZEpnpycdGlqyTOksNekoRaZ0lpoAmWGQMR3XGDMYmdOxnbEy" +
                    "qGM742RSx3bGy6iO7UyQWZ2l5mNmiDKsYw9Tkmkdt/+aIYu4znFvvA5F5HVu+l5WQ8vmkpod19KvYDaX1Ny4ln7TenNJzW+FL05kdmxfvAjtGmp3XMvZyY2J" +
                    "26Q7N+9/9oGQEkRox7YlitSOXS5JxHZsX7LM7djGFBnckcVsbqmZr3Re0/+jErO5pmbcuJh+NbO5p2b8uJiOw+RkeMd2xsv0ju1MkPGdu4yr05OXgot3X3r4" +
                    "q7tCS5TxnaMesjObu2qmbEXJZBngsUumyASP7Mzmupq12+DM5r6a/ZoG3PGdCFaGeI67i9zcWLNhXEzHmvEyxmM7E2SOx3YmyhyP7UySOZ67jExm8tJ0/t5/" +
                    "PHz7U6ElyxiPbUyRMR65ZDZ315zdBmc2t9ec2wpnrIzxGsRoJGebG2zuaxpwv+hsc4dtPZk8dwe3ucRmxrV0fHVQFDFegxaNx4fNLTa3FWOURYzXoEXh4LXZ" +
                    "3GIL2+DL5hZbHNfSr9ttbrGlrfDFihzPT483Neplc4utbIUvXuZ47IIJMsfz3A9QNvfYzFdaL2PntrnIZty4mH4fuW1ushk/LqZjzRSZ43nubn9zl83EbXBm" +
                    "c5nNpK1wxsogz1PfI2c219lM2QpnvAzy2DUTZJDnua/N3Nxos7ZVjEbT21xps651mFTEZBnk+en3DlWGqcggz3N32JtbbTZuhRgjgzzPDUI299ps3gpnnAzy" +
                    "2M54meSxnQkyyWM7E2WSx3YmySTPU48gm80dN+dbndHYqmxuubmv6cD9jipmiaAI3JejZwmhCNN32hoVkyWGInA32lmCKAIV/2CyRFEEbhqSJYyCPUYSR8H2" +
                    "RQIp2L5IJAXbFwmlCNQXRpoCVIpwGSc4/4q3xgOWosEZjQ5TgEsRuCfyCoApAjdWLECmCNR3UpkCaIpAxh4AmyJwI6ICcIpAfV2XKUCnYDsDeApuzdgB+BSB" +
                    "+uY7OwCgIlBf82kHIFQELi1jAERFoGacdgBGRaC+ncoOAKkI1BMhdgBKRaC+ud0OgKkIXDTEAJyKQI2I7ACgCrIzBkgVZGcMoCrYzgCrgu0MwCrC9I8vNKa2" +
                    "AVpF4FKCDOAqAhcTZCSvIlJ3/dZIYEWk7vqtkcSKyEXbGomsiNSzitZKZkWkporWSmgFuV6spFaQ55GV2Aq2L5JbEamvfrNWgivYvgC5IlKTPGsBXUHudhbY" +
                    "FZG7dbMAr4jUM3nWAb2CLQbwFZEaEVkH/IrIjYgcACzYzgDBIk6PiDTYrg4QFpEbETlgWJD7jAOIBdsZoFiwxQDGgjy1PXAs4vStmwr5FkAWkYtV9UCyiNwd" +
                    "tgeUReRmDx5YFpEbEXmAWURuROSBZsF2BnAWbGeAZ9EwtTW22R6AFpGKnbUBiBZkZwIgLch9JkimRXqkxREmU5BQi6qWfnMpSKpFVUu/z7uCxFqwx0hyLapa" +
                    "+iUzQYIt2L5IsgW7diXaguxLlGyLhnmkUS9Rwi3IYxSBbtFgjMaKFAFv0TCrNbpdBL6FEOMJeVUEwEVVTL/tfgTCBVsMIC6qYjp2GWBcNNSMihiAXJCdSUC5" +
                    "aHBGI8lLgLloEKPRZxJwLqpi+nXgBKCLhqmtcI7TJiBdNBSwRgdOgLogT+0ErAshJhAWygSwi6qYfnuUBLSLqph+q3YC3EVVTLcreDYD7qLBGY0CzoC7aHBG" +
                    "RQzgLtjOAO6CPJsy4C7YzgDuoiqm38mzDLiLhgJWEQO4C/JykCXuIk/f9atokbiLqpZ+CWeRuAuyL0XiLjI3gSgSd5G5KVGRuAu2LxJ3wfZF8i7YvkjeBdsX" +
                    "ybvI3PCsAO+iYZBUmh3wLhpGScEZNwDvIlODEDcA7yJTN7VuAN5FVYzrJwZ4F5m6w3YD8C7YNQO8C7YzALxocEYhyXMDAC/YzgDwIlM/MXADAC/IU9sA8KLB" +
                    "GYVY0RkAXmTqxxfOAPAiT9/uayyUBoAXefqmVmWYAHjRIEalgAF4kadnDyrOAPAiU8MzZwB4kakRkTNAvGA7A8SLPD2V0agZC8QL8myyQLwg14wF4kWDGI0O" +
                    "bIF40VAzKs5I4kWhnrFyVhIvCnXX76wkXhTu3tZK4gXbF0m8KNSUyFlJvCD74iTxguyLk8QLti+SeMH2RRIv2L4A8aJQkzzngHhRqJ+hOAfEizL9rIzKMAHx" +
                    "okGMwic6zgHxonCDEAfEi8INzzwQL8jOeCBesJ0B4kVDAWskeR6IF4UbEXkgXrBrBogXZXpepfG86YF4UbixogfiBXs2AfGiUA9hOA/EC7KYAMSLwt3uByBe" +
                    "FG5EFIB4wXYGiBdsZ4B4wXYGiBdsZ4B4wXYGiBeFesLVBSBeNDijIgaIFw0dWGPVjpJ4sdxXTj2oolEzUSIv6mK6vYbCRcm8qIvpN7WjhF7UxfSb2lFSL+jO" +
                    "SOwF3RnJvWgp4EtgvD9864+LD28KLZJ7QTdGgi/oJSPJF2xnEqAv2NYkYF/QvQH4hVTD2G0noF/U1fT72DYB/qKupmPdAP/iL1Oz7nR1Mev/NpIX/eLjxe03" +
                    "hBbAX9CdAf5FS9Vcwppw8eFnizf+KMQA/4JuDQAwWqa3RsyYgYDB9iYDAqPFG41mk4GBQfcGIBgt3mjsoDJQMKQaxkG9DBiMupp+S2YGDkZdTce6ARAG3Rsg" +
                    "YdC9ARRGXY3a1uX+Zz99+OLdR2IKoDDY1hRgYbDLpgAMg+4N0DDo3gAOo6UVq6gBHkbLlNKIs4oEYiz3d1PP7GmsUkUSMVrEqIyTRGLUxfQ79V8kE6Muptvs" +
                    "9oOEYtTF2H5iJBWD7ozEYtCdkVwMujMSjEF3RpIx6M4AGoOuBtgYdTXdXnzoB4BjSDWE83t+ADpGXU23gzXeAB6jrqbflDLAx6ir6VfFBgAZLWoUYhJvgJBB" +
                    "9wYQGS1zSiFe8wYYGXRvAJJBn1NAyWjxRqXfACajpW4U9i3eACeDXTcWQBlSDWGH6S2QMupqfD81gMqoq+k3pyywMupqOtYNwDLo3gAtg+4N4DLo3gAvg+4N" +
                    "ADPo3gAxo6XfaOwZHCAzWrzRWKccMDNa1Kh4I6EZy4dk4g1d7yQ1oy6m327KSWxGXUy/CeUkN4MuRoIz6mL6NT4nyRl0ZyQ6g+6MZGewnfESnsF2xkt6RosY" +
                    "jT7jAZ/R0vU0WrAHfgZ9oACgIdUQ7op5DwSNupp+65MHhEZdTcf5DQwNuhqAaNTVdKxioGiwvQmA0WjxRiMJDcDRoHsDII26mn6pTQCSBt0bQGm0dD8VNcDS" +
                    "YM/wADANqYaxpwtA06ir6VjFgNOgewM8jRY1Gt5EAGrU1fSr4ghEDbo3gNSgewNMDbo3ANVgz6kIVI2WfqOiBrAadTWu30gBV4NeNxKssdzHEFmwPkqyRl1M" +
                    "t1OPPkm0RoszGsOUJFuD7oyEa7SIUXFG0jXqYvqtCUniNepiut288UnyNejOSMBGS81oLAhJEjZahklFDCA2WtSozG1gbLDbXgbIhlTDOE+SgbLRokYj7cuA" +
                    "2air6fcckYGzUVfTr9tkAG20qNFoxBlIG3U1rp8aQG20VLHG2b4MrA16FQNso0WNygwH2kbLSGmsUwVwG+wZXoC30TJSKt4AcEOqIbzv3RcgbrSoUfEGkBt1" +
                    "Nf16cQHmRosalSoG6EbLSKmoAeoG3RvAbtC9Ae4G2ZswAHijRY1CvwkDkDfqakI/bwC9QVcD7I26mm4vTQ2DhG8sdw5fiIn9F4YwSPpGXYzrJ0biN1rEqAyT" +
                    "5G/UxYR+s1sCOOjDJAkcLQWsMbeNRHDUxfRbEoxkcNCdkRAOds0YSeFgT20DGI6Wua0yTsDhqKvx/dQAiEOqSYyyARJHXU3H6Q0ojhZvFHZRwQCLo66m2+dR" +
                    "wQKMgz1SFmgcdTX9OrEFHEeLNwppX7DA42jxRmOGWwBy0OsGiBwtc0pFDSA56N4Ak6Oupt+qaQHKIdVkhjdA5air6fbRanCA5WB744DLUVfTrxc7AHPQvQEy" +
                    "B90bQHPQvQE2B90bgHPQvQE6R0u/UVEDeI6WkdJ4EnXA56ir6XbSMHgAdITpmzsNazwAOgJ33+sB0BG4kYAHQEeDGI20zwOgI3BjJA+AjsAN2DwAOtjOAKGD" +
                    "7QwQOtjOAKGjoc9oPA0HJHSQrQlI6GjoNBoZUkBCR+DuMAMSOgJ39x2Q0BG4+VpAQgd7pJDREbj5WkBGB9sbhHSwvUFIR0MVa2SPESEdDd5odL+IkA5y3USE" +
                    "dDSo0XgGjQjpaBgpFTUI6QjTd98qahDSEbhZQERIR+DmJBEhHWxvENLB9gYhHeEykokGSMd/Pfj9S4/EJIR0kK1JCOkgl01CSAfbG4R0sL1BSAdbDUI6GtRo" +
                    "PKQngHRE7r43AaSjQYyKMwDpiNzkMQGkI3IDgQyQjshNkTJAOuL0tFrj8TwDpIPtDEA62DUDkA62MwDpYDuDkI4GazS6XkZIB3lByAjpiNOjABU1COmI0w/T" +
                    "aYxUQUhH5EYBBSEdkRshFYR0sL1BSAfbG4R0NKjRiNcKQjrY3iCkg103COlge4OQDrY3COmI04/2KcRrcUBIR6TuvuOAkI5I3e/GASEdbDUI6WCPFEI62N4g" +
                    "pIPtDUI6GtQo7OzigJAOtjcI6WDXDUI6IvUocTQI6SCPlEFIR6Qe7YsGIB1p+lZTY9E0AOlI03fhCrupaADSkaiRQDQA6UjUsCQagHQ0iNFowwYgHWxnANKR" +
                    "pp+LuoTPxh6+eG9x+w2hBRgdZGMsMDrIk8kCo4PtDDI62NYgoyNRzxBHi4yORN3uRouMjkTd7kaLjA62N8joYHuDjA62N8joSJdxInT6qvD2q+dvvf1IjENE" +
                    "B9kah4gOctk4RHSwvUFEB9sbRHQ0eKPxbO4Q0ZGmZ30a+0uHiI7E3V86RHSk6UmAxuO5Q0SHULN8xqgNlVUtHGR01OWYqhyNsfII6ajLsf3kIKWjLsdV5WjU" +
                    "jkdMB90d5HTQawdBHXR3kNTRUjsaWwePqI6WvqMiB1kd7DboAdaRp++ANZZPD7COTH11VAwA68jUN+bFALCOzI0GAsA6Mjc1CQDrYDsDsA62MwDrYDsDsA62" +
                    "MwDrYDuDsI5MfadCDAjryNQzfjEirCNPP1WnMVIRYR2ZeoksRoR1ZG4qEBHWkbmJSURYB9sbhHWwvUFYB9sbhHXk6ZGoxj4zIqyD7Q3COjL1Cm1MCOvI1BPO" +
                    "MSGsg60GYR15+ikXlZFCWEemXi+OCWEdmRyWJKR1ZHJYkhDXQXcHeR10dxDYQXcHiR1sdzIiO9juZGR20N1BaEeLOxpbmYzUjroc388dwHYUbu6XAdtRqHd7" +
                    "YwZsR+GGAxmwHYWbiGbAdrCdAWxH4QZKBbAdZGcKYDvYzgC2g+0MYDvKZRzK/CvEILajcD9gKIjtKFS+SiyI7SjUu72xILajcO+TFcR2FO6Jl4LYjkJNTNKA" +
                    "2I5CTdrSgNgOtjeI7WB7g9gOtjeI7SjUV3WmAbEd3BmeBsR2NHij0P3SgNiOQs1E04DYjlJL2kKv9DoNiO1oUKPhjUFsR+HGAckgt6Nww5JkENxBdwfJHXR3" +
                    "EN1RLiUsmXxS/uLdlxZ/uCvUILqDbg6yO+ilg/AOujtI76C7g/iOFjkaTdlKfsfyC+Il32Qlv6Mupttb9JKV/I66GNdPjOR31MX0e+Sykt/RIkbjGcdKfkdd" +
                    "TL9NjJX8jpbZpCJG8jvozkh+R8tsUkj0k5X8DnafccDvYI+TA35HizcaW00H/A6phhD7JQf8jrqajt4Av6Oupt8C5YDf0eKNihrgd9TV9GvEDvgddTUdZzjw" +
                    "O+jeAL+D7Y0HfkdLFWs8Z3ngd7D7jQd+R0svVhkp4He0dD+VkQJ+h1RDeMVW8sDvqKvpOKeA3yHVMK7bJQ8AjxE5/eIADwSPETn9whIPCA+6OwEgHnR3AlA8" +
                    "+O4AxoPvDnA8+O4AyKOp72gsnwFIHiNyfD93JMpj+YTxhZpSrxyj6o1EedTFuH5iJMqjLsZXxWgkFUGiPOpiQjdnokR5sGsmSpRHixiN2RQlyqMuxvYTI1Ee" +
                    "dGckyqOlgDWe/aJEedBnE6A8WtRoNJoIKA+6N4DyoKsBlIdUsxy8r53fGlWcgOUxIqdfI04A8xiR4/q5AzSPETm+nzuA82gaLI12nIDn0VTKKnIA6MF3B4ge" +
                    "fHcA6TEip18bTMD04MsBqAe9DWagejTJ0WiDGbAeTYOlIge4HiCHcPcuZQB7jMjpdwgnA9mjSY5G38mA9mgaLBU5wPbguwNwD747QPfguwN4jyY5GtuaAnyP" +
                    "ETm+nxwAfIzI6fYmxFQk4WO5rk+NujR6cpGEj7qYfiFgkYSPuph+jxZFEj7oYiTho0WMRrcpkvBRF9Pvcb1IwgfdGYn4IDuTB4n4IDuTB4n4IM+mPADio6UF" +
                    "K6wHeQDER0sPVlgs8wCIjxZvVNQA4kOq+cZsQKVwgPExIif0kwOQjxE5HacVUD74gwWYjyY5Gh3QAOijabBU5ADpo6mUFS4UZAOoD747wPrg1w7APkbkdHtO" +
                    "zwZoH/SJbgD3Qe/KBngffDkA/AA5tv8RmGwA+NEkR+PhywLwY0ROt1wpWwB+jMix/eQA8IPvDgA/+O4A8IPvDgA/RuSEfjMLgB9Ng6XRBi0AP0bkuF6ZZLYS" +
                    "+LGsa2Lulq0EftTF9FvNnQR+1MX4fmIk8KMupt9DqZPAj5ZhUhEjgR90ZyTwg+6MBH7QnZHAD7ozEvhBdwaAHy0tWGM9cAD8qKvpt2fwAPxgN2EPwA+phrLd" +
                    "9ED8GJHT7ehd9oD8GJHT7Uhr9sD8aJKjkSt5gH6MyOnXdDxQP5pKWUUOYD/47gD3g+8OgD/o7gQgfzTJ0ZhZAdAfTRNdoysHYH80uaMiB+AfI3L6LaEB6B8g" +
                    "55t2vyqlDPSPETn9kpMA9I8ROf1ypQD0D747QP/guwP0D7o7EegfdHci0D/47gD9o0mORhuMQP/gD5akfyxXLuaeL0r6R11MvzMxUdI/6mL6PXhFSf+oi+n3" +
                    "UBol/YPujKR/sJ1Jkv7RUsCX8K7axWu/XNz74OE/vX/x5v8KRRIBQrdHIkDYhZMkAoTuDCBA6NYAAoS9KiRAgLSMlIoaQIBINd8YvWlsgRMwQJrkaERvCSAg" +
                    "I3L6TasMFJAROf3mVQYMCN8d4IDw3QEQSFMpX8Lyef/ez+5/9sHFm+/C9MqAA+FbBDwQfgEBEITvDhBB+O4AEmRETr/9TAYmSJM7GutoASgIyGGkBAWgICNy" +
                    "+m3LC0BB+O4AFITvDkBBRuQ4LSjI4s6dB+/8cvGf7y0+el9oAjQI3yJAg/ALCNAgfHcADcJ3B9AgTXIUWnMZAA0SqAcDywBokEC9tVcGQIOwxQAaJFAjgzIA" +
                    "GiRQ05QyABqE7QygQdjOABqE7QygQdjOABqE7IxBNEigBnDFIBqkwRuFxKsYRIME6psli0E0SOAesigG2SCBe0uvGISD0OUgHSRwA4JiEA8SuOcUi0E+CN0d" +
                    "BIQEbrhULBJC2O5YRITQ3UFGCN0dhIQE7km8YpESQpeDmJDATQKLRU5IaEhzVOQgJyRws4FikRMSuMlJscgJYbvjkBPCdschJ4TuDnJC6O4gJ4TuDnJCQsNl" +
                    "ZYWPzotDTkhoeCuBihzghETq9avigBMSqSdAigNOCNsZ4ISQxXjghERuhuKBExK56ZIHTgjbGeCEsJ0BTgjbGeCERO4nHx45IZF6HLp45IREbiTpkRNCXhE8" +
                    "ckIieUMVkBMSyZlkQE5IJIeAATkhkXtpsATkhERychKQExLJuVJATgjdHeSE0N1BTkjknkUuATkhkftWghKQExLJqVtETgi770TkhLTMLI3tZkROSCSHgBE5" +
                    "IZG8GY/ICYnk5CQiJySSc6WInBC6O8gJobuDnBC6O8gJYbuTkBNCl4OckEg6Cfj48qdP9k9nu489O7txsP7tj/72+TPX5ycH86Mr85ULJ3tHT67+sD+/enB0" +
                    "cLbSO3vixt7Rtas3fnR6fHRttvzpvZOj+f4Pnjw4PJztzlb/5NVvtu2/ee/oR8dXrx5cmZ983S92f9EvPr1yfOOs9msf35ldPT55am/9vY+ZHbvjHsfvW/r1" +
                    "+HLIrh+fnG2+eGp+erp3bb78anbx0Vvnb/zm4tNfP3z+hW+5YfjW+Qufnr9x88+f/+uDW787v/nqg1u/e/DJvz24dXPx8suL5z9f/PNr57duLz5858HPP7r/" +
                    "P7/60/MvznZm57/96cV/372489vF79/50/MvLl5+dfH6rx/++yuLn71/8X9/uP/Z8+f33rv49KUH995c3Pr4z5+/tnjl9Qc3f7O489b5L15Z3L69/CWP78yO" +
                    "T/bnJyuFz/0/KmufcSscAgA=";
                break;
            case "outbound":
                expectedSha = "D25B02EA33673A7B6B41B8203AC9CA6DAB745C6FF7229390F334A74C29E8DF93";
                encoded =
                    "H4sIAAAAAAACCs2dXY9dxZmF/0p0rlvWru+qvs3NzN0omcxcICvquI9ND023dbrNoCAkYABhBRCZCUnQWDjzgSBK+DAeJsTDTH5MfNz2Vf7C6PQ54HeJ2rAr" +
                    "6bfWuUFu7G4vr3rrrV3rVO3nudnJ3jPzv5svTg6Oj2a7Zmf29N71v96f7c5m57/8+4P90ydnu8P5F381P7j25OmXX31v/szB+ruGndnixuF8MdudfX++ONg7" +
                    "PPjx3unB8Wxndrw4uHaw/hM/OlicPvm9+bXz7/A7syePn56v/iKXws7sdHF8fP1ktuvDsDM7mj97+t29xf7qd/3ObO/w8G+Ojw9PfnB0eHzlqfn+bPfq3uHJ" +
                    "fGc2P9y7frL62g3x0rBz/i/Z/8Hplb89uPLUyVrV/OT4xuLK/GS2+9zs2mLv5GT1h4fhkt9ZfblS5ldfxp3Zyenx0Xy2a7IJl9zO7GCxklmGdCmGndm148P9" +
                    "2a6JZrhkd2b/sLc/P/85z+/MDveO9k9mu088NzvYP/8rnz3/7483f/21L/05nD8zP5zthvW/e/Vd5//7qYPVL9zO7Pgfj8S/7Mre9YPTvcOvvl59z1df/Ghx" +
                    "sH/t8ZdHe6vfmz36lxeXt+7Nnt9ZKzHnSkyrkuEClJx9+tHZzz75Sok9V2LHldheSty5EjeuxPdS4s+V+HElpqrEXoSS23dlnYRzJWELRieeK4mtnigoSedK" +
                    "0hZ4ks+V5C3wpJwrKeNKXFWJuQAlD9+4CZ1t3WTNsAW9zWzarNmCWjHrRmu+1mnNt/ii0FXMutUaN66lW68162Zr/LiWfmO0brcmtI6RhpZ1wzVxXEu37mLW" +
                    "LdekbRijddM1eRt8WbddU8a11PuuvwAtDz69++CzFx8/x637rh22oL/Ydd+1pnWMFNYju3nAbe67Ck/9dt13rWudRxpa1n3Xfq3v2m+pXY09yLrv2jCuxXer" +
                    "3XXftXFcS7+92brv2rQNWtZ91+ZxLd36rl33XVvGtfTbta77rhu2wBe37rvObMGcduu+6+wW1K7bRAuu1ReFvuvWfdf5baiXdd91odUXhXXaRRHOGW4QlUQ6" +
                    "Z6bvphVWI5dFPGemb6Y1BqiIfI4rxQ8ioOPWijcioTPUDMhbEdGRXXEioyO74kVIR3YliJSOLCXKlI7b43ySKR25WrJM6cz03aJGlysypTPUNCoMMqUz05MO" +
                    "DS1GpnSGmgAFK1M6Q03GgpMpHdkXL1M6Mz1hVkjGQpApHdmXKFO6Bi0any0mmdI1zGmFXheyTOnI/aXIlK5BS7gALcs7d5a3xKeug0zpDHVHH41M6cz01EXD" +
                    "FytTOkPdRUcnUzqyFi9TOkNNxmKQKZ2Znupq1EuUKR3ZlyRTOnK9ZJnSNfQXhfUoFpnSNfiioCUNMqXj9t1kZEpnqElqsiKls9O3sBpD5ERKZ6k72ORFSmen" +
                    "H11TiJdTECmdpWYMKYqUznKPjCWR0pFdySKlI7tSRErHdSUPIqXjupKNSOnIrliZ0lnqub7sZErHbf3Zy5TOUpOxHGRKZ6fvFjV8iTKls9SkIyeZ0llqMpaz" +
                    "TOnIvhSZ0nF9KYNM6Rq0KKR0xciUjuyLlSkdt16Kkyldgy8K/aV4mdJxe10JMqXjrgElypTOTt+hKazTJcmUzk5POjS0ZJnSWWrSUYpM6Sw1ATLDIGM6rjFm" +
                    "MDKnYztjZVDHdsbJpI7tjJdRHduZILM6S83HzBBlWMcepiTTOm7/NUMWcZ3j3ngdisjr3PS9rIaWzSU1O66lX8FsLqm5cS39pvXmkprfCl+cyOzYvngR2jXU" +
                    "7riW08WNidukOzfvf/6hkBJEaMe2JYrUjl0uScR2bF+yzO3YxhQZ3JHFbG6pma91XtP/oxKzuaZm3LiYfjWzuadm/LiYjsPkZHjHdsbL9I7tTJDxnbuIq9OT" +
                    "l4KH77386Jd3hZYo4ztHPWRnNnfVTNmKkskywGOXTJEJHtmZzXU1a7fBmc19NfsNDbjjOxGsDPEcdxe5ubFmw7iYjjXjZYzHdibIHI/tTJQ5HtuZJHM8dxGZ" +
                    "zOSl6ez9/3j0zmdCS5YxHtuYImM8csls7q45uw3ObG6vObcVzlgZ4zWI0UjONjfY3Dc04H7R2eYO23oyee4ObnOJzYxr6fjqoChivAYtGo8Pm1tsbivGKIsY" +
                    "r0GLwsFrs7nFFrbBl80ttjiupV+329xiS1vhixU5np8eb2rUy+YWW9kKX7zM8dgFE2SO57kfoGzusZmvtV7Gzm1zkc24cTH9PnLb3GQzflxMx5opMsfz3N3+" +
                    "5i6bidvgzOYym0lb4YyVQZ6nvkfObK6zmbIVzngZ5LFrJsggz3Nfm7m50WZtqxiNpre50mZd6zCpiMkyyPPT7x2qDFORQZ7n7rA3t9ps3AoxRgZ5nhuEbO61" +
                    "2bwVzjgZ5LGd8TLJYzsTZJLHdibKJI/tTJJJnqceQTabO27OtzqjsVXZ3HJz39CB+x1VzBJBEbgvR88SQhGm77Q1KiZLDEXgbrSzBFEEKv7BZImiCNw0JEsY" +
                    "BXuMJI6C7YsEUrB9kUgKti8SShGoL4w0BagU4SJOcP4Fb40HLEWDMxodpgCXInBP5BUAUwRurFiATBGo76QyBdAUgYw9ADZF4EZEBeAUgfq6LlOATsF2BvAU" +
                    "3JqxA/ApAvXNd3YAQEWgvubTDkCoCFxaxgCIikDNOO0AjIpAfTuVHQBSEagnQuwAlIpAfXO7HQBTEbhoiAE4FYEaEdkBQBVkZwyQKsjOGEBVsJ0BVgXbGYBV" +
                    "hOkfX2hMbQO0isClBBnAVQQuJshIXkWk7vqtkcCKSN31WyOJFZGLtjUSWRGpZxWtlcyKSE0VrZXQCnK9WEmtIM8jK7EVbF8ktyJSX/1mrQRXsH0BckWkJnnW" +
                    "ArqC3O0ssCsid+tmAV4RqWfyrAN6BVsM4CsiNSKyDvgVkRsROQBYsJ0BgkWcHhFpsF0dICwiNyJywLAg9xkHEAu2M0CxYIsBjAV5anvgWMTpWzcV8i2ALCIX" +
                    "q+qBZBG5O2wPKIvIzR48sCwiNyLyALOI3IjIA82C7QzgLNjOAM+iYWprbLM9AC0iFTtrAxAtyM4EQFqQ+0yQTIv0WIsjTKYgoRZVLf3mUpBUi6qWfp93BYm1" +
                    "YI+R5FpUtfRLZoIEW7B9kWQLdu1KtAXZlyjZFg3zSKNeooRbkMcoAt2iwRiNFSkC3qJhVmt0uwh8CyHGE/KqCICLqph+2/0IhAu2GEBcVMV07DLAuGioGRUx" +
                    "ALkgO5OActHgjEaSlwBz0SBGo88k4FxUxfTrwAlAFw1TW+Ecp01AumgoYI0OnAB1QZ7aCVgXQkwgLJQJYBdVMf32KAloF1Ux/VbtBLiLqphuV/BsBtxFgzMa" +
                    "BZwBd9HgjIoYwF2wnQHcBXk2ZcBdsJ0B3EVVTL+TZxlwFw0FrCIGcBfk5SBL3EWevutX0SJxF1Ut/RLOInEXZF+KxF1kbgJRJO4ic1OiInEXbF8k7oLti+Rd" +
                    "sH2RvAu2L5J3kbnhWQHeRcMgqTQ74F00jJKCM24A3kWmBiFuAN5Fpm5q3QC8i6oY108M8C4ydYftBuBdsGsGeBdsZwB40eCMQpLnBgBesJ0B4EWmfmLgBgBe" +
                    "kKe2AeBFgzMKsaIzALzI1I8vnAHgRZ6+3ddYKA0AL/L0Ta3KMAHwokGMSgED8CJPzx5UnAHgRaaGZ84A8CJTIyJngHjBdgaIF3l6KqNRMxaIF+TZZIF4Qa4Z" +
                    "C8SLBjEaHdgC8aKhZlSckcSLQj1j5awkXhTqrt9ZSbwo3L2tlcQLti+SeFGoKZGzknhB9sVJ4gXZFyeJF2xfJPGC7YskXrB9AeJFoSZ5zgHxolA/Q3EOiBdl" +
                    "+lkZlWEC4kWDGIVPdJwD4kXhBiEOiBeFG555IF6QnfFAvGA7A8SLhgLWSPI8EC8KNyLyQLxg1wwQL8r0vErjedMD8aJwY0UPxAv2bALiRaEewnAeiBdkMQGI" +
                    "F4W73Q9AvCjciCgA8YLtDBAv2M4A8YLtDBAv2M4A8YLtDBAvCvWEqwtAvGhwRkUMEC8aOrDGqh0l8WK1r5x6UEWjZqJEXtTFdHsNhYuSeVEX029qRwm9qIvp" +
                    "N7WjpF7QnZHYC7ozknvRUsAXwHh/9PYflh/dFFok94JujARf0EtGki/YziRAX7CtScC+oHsD8AuphrHbTkC/qKvp97FtAvxFXU3HugH+xZ+nZt3p6mLWvzeS" +
                    "F/38k+XtN4UWwF/QnQH+RUvVXMCa8PCjz5dv/kGIAf4F3RoAYLRMb42YMQMBg+1NBgRGizcazSYDA4PuDUAwWrzR2EFloGBINYyDehkwGHU1/ZbMDByMupqO" +
                    "dQMgDLo3QMKgewMojLoata3L/c9/8uilu4/FFEBhsK0pwMJgl00BGAbdG6Bh0L0BHEZLK1ZRAzyMlimlEWcVCcRY7e+mntnTWKWKJGK0iFEZJ4nEqIvpd+q/" +
                    "SCZGXUy32e0HCcWoi7H9xEgqBt0ZicWgOyO5GHRnJBiD7owkY9CdATQGXQ2wMepqur340A8Ax5BqCOf3/AB0jLqabgdrvAE8Rl1NvyllgI9RV9Ovig0AMlrU" +
                    "KMQk3gAhg+4NIDJa5pRCvOYNMDLo3gAkgz6ngJLR4o1KvwFMRkvdKOxbvAFOBrtuLIAypBrCDtNbIGXU1fh+agCVUVfTb05ZYGXU1XSsG4Bl0L0BWgbdG8Bl" +
                    "0L0BXgbdGwBm0L0BYkZLv9HYMzhAZrR4o7FOOWBmtKhR8UZCM1YPycQbut5JakZdTL/dlJPYjLqYfhPKSW4GXYwEZ9TF9Gt8TpIz6M5IdAbdGcnOYDvjJTyD" +
                    "7YyX9IwWMRp9xgM+o6XrabRgD/wM+kABQEOqIdwV8x4IGnU1/dYnDwiNupqO8xsYGnQ1ANGoq+lYxUDRYHsTAKPR4o1GEhqAo0H3BkAadTX9UpsAJA26N4DS" +
                    "aOl+KmqApcGe4QFgGlINY08XgKZRV9OxigGnQfcGeBotajS8iQDUqKvpV8URiBp0bwCpQfcGmBp0bwCqwZ5TEagaLf1GRQ1gNepqXL+RAq4GvW4kWGO1jyGy" +
                    "YH2UZI26mG6nHn2SaI0WZzSGKUm2Bt0ZCddoEaPijKRr1MX0WxOSxGvUxXS7eeOT5GvQnZGAjZaa0VgQkiRstAyTihhAbLSoUZnbwNhgt70MkA2phnGeJANl" +
                    "o0WNRtqXAbNRV9PvOSIDZ6Oupl+3yQDaaFGj0YgzkDbqalw/NYDaaKlijbN9GVgb9CoG2EaLGpUZDrSNlpHSWKcK4DbYM7wAb6NlpFS8AeCGVEN437svQNxo" +
                    "UaPiDSA36mr69eICzI0WNSpVDNCNlpFSUQPUDbo3gN2gewPcDbI3YQDwRosahX4TBiBv1NWEft4AeoOuBtgbdTXdXpoaBgnfWO0cvhQT+y8MYZD0jboY10+M" +
                    "xG+0iFEZJsnfqIsJ/Wa3BHDQh0kSOFoKWGNuG4ngqIvptyQYyeCgOyMhHOyaMZLCwZ7aBjAcLXNbZZyAw1FX4/upARCHVJMYZQMkjrqajtMbUBwt3ijsooIB" +
                    "FkddTbfPo4IFGAd7pCzQOOpq+nViCziOFm8U0r5ggcfR4o3GDLcA5KDXDRA5WuaUihpActC9ASZHXU2/VdMClEOqyQxvgMpRV9Pto9XgAMvB9sYBl6Oupl8v" +
                    "dgDmoHsDZA66N4DmoHsDbA66NwDnoHsDdI6WfqOiBvAcLSOl8STqgM9RV9PtpGHwAOgI0zd3GtZ4AHQE7r7XA6AjcCMBD4COBjEaaZ8HQEfgxkgeAB2BG7B5" +
                    "AHSwnQFCB9sZIHSwnQFCR0Of0XgaDkjoIFsTkNDR0Gk0MqSAhI7A3WEGJHQE7u47IKEjcPO1gIQO9kghoyNw87WAjA62NwjpYHuDkI6GKtbIHiNCOhq80eh+" +
                    "ESEd5LqJCOloUKPxDBoR0tEwUipqENIRpu++VdQgpCNws4CIkI7AzUkiQjrY3iCkg+0NQjrCRSQTDZCO/3rwu5cfi0kI6SBbkxDSQS6bhJAOtjcI6WB7g5AO" +
                    "thqEdDSo0XhITwDpiNx9bwJIR4MYFWcA0hG5yWMCSEfkBgIZIB2RmyJlgHTE6Wm1xuN5BkgH2xmAdLBrBiAdbGcA0sF2BiEdDdZodL2MkA7ygpAR0hGnRwEq" +
                    "ahDSEacfptMYqYKQjsiNAgpCOiI3QioI6WB7g5AOtjcI6WhQoxGvFYR0sL1BSAe7bhDSwfYGIR1sbxDSEacf7VOI1+KAkI5I3X3HASEdkbrfjQNCOthqENLB" +
                    "HimEdLC9QUgH2xuEdDSoUdjZxQEhHWxvENLBrhuEdETqUeJoENJBHimDkI5IPdoXDUA60vStpsaiaQDSkabvwhV2U9EApCNRI4FoANKRqGFJNADpaBCj0YYN" +
                    "QDrYzgCkI00/F3UBn409eune8vabQgswOsjGWGB0kCeTBUYH2xlkdLCtQUZHop4hjhYZHYm63Y0WGR2Jut2NFhkdbG+Q0cH2BhkdbG+Q0ZEu4kTo9FXhndfO" +
                    "3n7nsRiHiA6yNQ4RHeSycYjoYHuDiA62N4joaPBG49ncIaIjTc/6NPaXDhEdibu/dIjoSNOTAI3Hc4eIDqFm9YxRGyqrWjjI6KjLMVU5GmPlEdJRl2P7yUFK" +
                    "R12Oq8rRqB2PmA66O8jpoNcOgjro7iCpo6V2NLYOHlEdLX1HRQ6yOtht0AOsI0/fAWssnx5gHZn66qgYANaRqW/MiwFgHZkbDQSAdWRuahIA1sF2BmAdbGcA" +
                    "1sF2BmAdbGcA1sF2BmEdmfpOhRgQ1pGpZ/xiRFhHnn6qTmOkIsI6MvUSWYwI68jcVCAirCNzE5OIsA62NwjrYHuDsA62NwjryNMjUY19ZkRYB9sbhHVk6hXa" +
                    "mBDWkaknnGNCWAdbDcI68vRTLiojhbCOTL1eHBPCOjI5LElI68jksCQhroPuDvI66O4gsIPuDhI72O5kRHaw3cnI7KC7g9COFnc0tjIZqR11Ob6fO4DtKNzc" +
                    "LwO2o1Dv9sYM2I7CDQcyYDsKNxHNgO1gOwPYjsINlApgO8jOFMB2sJ0BbAfbGcB2lIs4lPkXiEFsR+F+wFAQ21GofJVYENtRqHd7Y0FsR+HeJyuI7SjcEy8F" +
                    "sR2FmpikAbEdhZq0pQGxHWxvENvB9gaxHWxvENtRqK/qTANiO7gzPA2I7WjwRqH7pQGxHYWaiaYBsR2llrSFXul1GhDb0aBGwxuD2I7CjQOSQW5H4YYlySC4" +
                    "g+4Okjvo7iC6o1xIWDL5pPzD915e/v6uUIPoDro5yO6glw7CO+juIL2D7g7iO1rkaDRlK/kdqy+Il3yTlfyOuphub9FLVvI76mJcPzGS31EX0++Ry0p+R4sY" +
                    "jWccK/kddTH9NjFW8jtaZpOKGMnvoDsj+R0ts0kh0U9W8jvYfcYBv4M9Tg74HS3eaGw1HfA7pBpC7Jcc8Dvqajp6A/yOupp+C5QDfkeLNypqgN9RV9OvETvg" +
                    "d9TVdJzhwO+gewP8DrY3HvgdLVWs8Zzlgd/B7jce+B0tvVhlpIDf0dL9VEYK+B1SDeEVW8kDv6OupuOcAn6HVMO4bpc8ADxG5PSLAzwQPEbk9AtLPCA86O4E" +
                    "gHjQ3QlA8eC7AxgPvjvA8eC7AyCPpr6jsXwGIHmMyPH93JEoj9UTxpdqSr1yjKo3EuVRF+P6iZEoj7oYXxWjkVQEifKoiwndnIkS5cGumShRHi1iNGZTlCiP" +
                    "uhjbT4xEedCdkSiPlgLWePaLEuVBn02A8mhRo9FoIqA86N4AyoOuBlAeUs1q8L5xfmtUcQKWx4icfo04AcxjRI7r5w7QPEbk+H7uAM6jabA02nECnkdTKavI" +
                    "AaAH3x0gevDdAaTHiJx+bTAB04MvB6Ae9DaYgerRJEejDWbAejQNlooc4HqAHMLdu5QB7DEip98hnAxkjyY5Gn0nA9qjabBU5ADbg+8OwD347gDdg+8O4D2a" +
                    "5GhsawrwPUbk+H5yAPAxIqfbmxBTkYSP1bo+NerS6MlFEj7qYvqFgEUSPupi+j1aFEn4oIuRhI8WMRrdpkjCR11Mv8f1IgkfdGck4oPsTB4k4oPsTB4k4oM8" +
                    "m/IAiI+WFqywHuQBEB8tPVhhscwDID5avFFRA4gPqeZbswGVwgHGx4ic0E8OQD5G5HScVkD54A8WYD6a5Gh0QAOgj6bBUpEDpI+mUla4UJANoD747gDrg187" +
                    "APsYkdPtOT0boH3QJ7oB3Ae9KxvgffDlAPAD5Nj+R2CyAeBHkxyNhy8LwI8ROd1ypWwB+DEix/aTA8APvjsA/OC7A8APvjsA/BiRE/rNLAB+NA2WRhu0APwY" +
                    "keN6ZZLZSuDHqq6JuVu2EvhRF9NvNXcS+FEX4/uJkcCPuph+D6VOAj9ahklFjAR+0J2RwA+6MxL4QXdGAj/ozkjgB90ZAH60tGCN9cAB8KOupt+ewQPwg92E" +
                    "PQA/pBrKdtMD8WNETrejd9kD8mNETrcjrdkD86NJjkau5AH6MSKnX9PxQP1oKmUVOYD94LsD3A++OwD+oLsTgPzRJEdjZgVAfzRNdI2uHID90eSOihyAf4zI" +
                    "6beEBqB/gJxv2/2qlDLQP0bk9EtOAtA/RuT0y5UC0D/47gD9g+8O0D/o7kSgf9DdiUD/4LsD9I8mORptMAL9gz9Ykv6xWrmYe74o6R91Mf3OxERJ/6iL6ffg" +
                    "FSX9oy6m30NplPQPujOS/sF2Jkn6R0sBX8C7apev/2J578NH//TBw7f+VyiSCBC6PRIBwi6cJBEgdGcAAUK3BhAg7FUhAQKkZaRU1AACRKr51uhNYwucgAHS" +
                    "JEcjeksAARmR029aZaCAjMjpN68yYED47gAHhO8OgECaSvkCls/79356//MPH771HkyvDDgQvkXAA+EXEABB+O4AEYTvDiBBRuT0289kYII0uaOxjhaAgoAc" +
                    "RkpQAAoyIqfftrwAFITvDkBB+O4AFGREjtOCgizv3Hnw7i+W//n+8uMPhCZAg/AtAjQIv4AADcJ3B9AgfHcADdIkR6E1lwHQIIF6MLAMgAYJ1Ft7ZQA0CFsM" +
                    "oEECNTIoA6BBAjVNKQOgQdjOABqE7QygQdjOABqE7QygQcjOGESDBGoAVwyiQRq8UUi8ikE0SKC+WbIYRIME7iGLYpANEri39IpBOAhdDtJBAjcgKAbxIIF7" +
                    "TrEY5IPQ3UFASOCGS8UiIYTtjkVECN0dZITQ3UFISOCexCsWKSF0OYgJCdwksFjkhISGNEdFDnJCAjcbKBY5IYGbnBSLnBC2Ow45IWx3HHJC6O4gJ4TuDnJC" +
                    "6O4gJyQ0XFZW+Oi8OOSEhIa3EqjIAU5IpF6/Kg44IZF6AqQ44ISwnQFOCFmMB05I5GYoHjghkZsueeCEsJ0BTgjbGeCEsJ0BTkjkfvLhkRMSqcehi0dOSORG" +
                    "kh45IeQVwSMnJJI3VAE5IZGcSQbkhERyCBiQExK5lwZLQE5IJCcnATkhkZwrBeSE0N1BTgjdHeSERO5Z5BKQExK5byUoATkhkZy6ReSEsPtORE5Iy8zS2G5G" +
                    "5IREcggYkRMSyZvxiJyQSE5OInJCIjlXisgJobuDnBC6O8gJobuDnBC2Owk5IXQ5yAmJpJOAl1ffvdg/me0+8dzsxsH6pz/+2+fPXp8vDuZHV+bnLiz2jp46" +
                    "/8X+/OrB0cHpud7Zkzf2jq5dvfHDk+Oja7PVd+8tjub733/q4PBwtjs7/yef/2Tb/pP3jn54fPXqwZX54pt+sPuzfvDJleMbp7Ufe3lndvV48fTe+s8+YXbs" +
                    "jruMf27l1+XVkF0/Xpxuvnh6fnKyd22++mr28OO3z9789cPPfvXohRe/44bhO2cvfnb25s0/ffGvD2799uzmaw9u/fbBp//24NbN5SuvLF/4YvnPr5/dur38" +
                    "6N0HP/v4/v/88o8vvDTbmZ395icP//vuwzu/Wf7u3T++8NLyldeWb/zq0b+/uvzpBw//7/f3P3/h7N77Dz97+cG9t5a3PvnTF68vX33jwc1fL++8ffbzV5e3" +
                    "b69+yOWd2fFif75YD+/p3uLa/HQ1I8tK+tN7B0cHR9dmu/HSyp4bi80/2NhL5+jq0xuL9e+fV87zl5//f+rAvMxsHAIA";
                break;
            case "returning":
                expectedSha = "A1DC7CD48B0B01AFC901800C5E39704731D1A08CEA3C10216CF8948ACA46E591";
                encoded =
                    "H4sIAAAAAAACCs2dX5NcxXnGv4pqrqdUp/93721ukruUHScXNuVaawcxYdmldlc2ZYoqQTCWbFDAAf8BFSKJie0YMIJgMCh2VT5KrNldXekruGZnQO9TnAOn" +
                    "8b79DBcUK2mXR0+//fbpZ7rP78nJ4fZ3Z/84Ozic7+9Ntsx08tj243+3M9maTM7+85/mO0ePTLa6sy/+dja//MjRp199bfbd+eq7uunk4Mru7GCyNfn67GC+" +
                    "vTv//vbRfH8ynewfzC/PV3/iO/ODo0e+Nrt89h1+Onlk/7HZ8n/kUphOjg729x8/nGx5m6aTvdkTR3+zfbCz/F0/nWzv7v79/v7u4Tf2dvcvPTrbmWw9vL17" +
                    "OJtOZrvbjx8uv3bGXjTTs7/JzjeOLv3D/NKjhytVs8P9KweXZoeTrScnlw+2Dw8nW67ruou5i5/+k6bL31mK9F1nLlrjy6f/pOnk8Gh/bzbZMtmEi7EL08n8" +
                    "YKm/dOli7myYTi7v7+5Mtkz04aLv3Gf/+Onkn7d3Zmf/u6emk93tvZ3DydY3n5zMd86UPXH27++vVV7+1Mbd2Xdnu5OtsLJn+V1nv/zofPkfbjrZ/96eMODS" +
                    "9uPzo+3dz75efs9nX3znYL5z+cGXe9vL35vc+7enFzc/njw1XSkxZ0pMrZLuHJScvPfOycvvfqbEnimxw0psKyXuTIkbVuJbKfFnSvywEtOrxJ6HklvvyzoJ" +
                    "Z0rCBoxOPFMSaz1RUJLOlKQN8CSfKckb4Ek5U1KGlbheJeYclJy+cB0626rJmm4DeptZt1mzAbViVo3WfK7Tmi/xRaGrmFWrNW5YS7Nea1bN1vhhLe3GaNVu" +
                    "TagdIw0tq4Zr4rCWZt3FrFquSZswRquma/Im+LJqu6YMa+nvu/4ctBy/9/7xB08/eI5b9V3bbUB/sau+a03tGCmsR3b9gFvddxWe+u2q71pXO480tKz6rv1c" +
                    "37VfUrsae5BV37VhWItvVrurvmvjsJZ2e7NV37VpE7Ss+q7Nw1qa9V276ru2DGtpt2td9V3XbYAvbtV3ndmAOe1WfdfZDahdt44WXK0vCn3Xrfqu85tQL6u+" +
                    "60KtLwrrtIsinDPcICqJdM6M300rrEYui3jOjN9MawxQEfkcV4rvREDHrRVvREJnqBmQtyKiI7viREZHdsWLkI7sShApHVlKlCkdt8f5JFM6crVkmdKZ8btF" +
                    "jS5XZEpnqGlU6GRKZ8YnHRpajEzpDDUBClamdIaajAUnUzqyL16mdGZ8wqyQjIUgUzqyL1GmdBVaND5bTDKlq5jTCr0uZJnSkftLkSldhZZwDloWt28vbopP" +
                    "XTuZ0hnqjj4amdKZ8amLhi9WpnSGuouOTqZ0ZC1epnSGmozFIFM6Mz7V1aiXKFM6si9JpnTkeskypavoLwrrUSwypavwRUFL6mRKx+27yciUzlCT1GRFSmfH" +
                    "b2E1hsiJlM5Sd7DJi5TOjj+6phAvpyBSOkvNGFIUKZ3lHhlLIqUju5JFSkd2pYiUjutK7kRKx3UlG5HSkV2xMqWz1HN92cmUjtv6s5cpnaUmYznIlM6O3y1q" +
                    "+BJlSmepSUdOMqWz1GQsZ5nSkX0pMqXj+lI6mdJVaFFI6YqRKR3ZFytTOm69FCdTugpfFPpL8TKl4/a6EmRKx10DSpQpnR2/Q1NYp0uSKZ0dn3RoaMkypbPU" +
                    "pKMUmdJZagJkuk7GdFxjTGdkTsd2xsqgju2Mk0kd2xkvozq2M0FmdZaaj5kuyrCOPUxJpnXc/mu6LOI6x73x2hWR17nxe1kNLetLanZYS7uCWV9Sc8Na2k3r" +
                    "9SU1vxG+OJHZsX3xIrSrqN1hLUcHV0Zuk25fv/vR20JKEKEd25YoUjt2uSQR27F9yTK3YxtTZHBHFrO+pWY+13lN+49KzPqamnHDYtrVzPqemvHDYhoOk5Ph" +
                    "HdsZL9M7tjNBxnfuPK5Oj14KTt989t7P3xdaoozvHPWQnVnfVTNlI0omywCPXTJFJnhkZ9bX1azdBGfW99XsFzTghu9EsDLEc9xd5PrGmg3DYhrWjJcxHtuZ" +
                    "IHM8tjNR5nhsZ5LM8dx5ZDKjl6aTX/3nvV98ILRkGeOxjSkyxiOXzPrumrOb4Mz69ppzG+GMlTFehRiN5Gx9g819QQNuF52t77CtJpPn7uDWl9jMsJaGrw6K" +
                    "Isar0KLx+LC+xeY2YoyyiPEqtCgcvDbrW2xhE3xZ32KLw1radbv1Lba0Eb5YkeP58fGmRr2sb7GVjfDFyxyPXTBB5nie+wHK+h6b+VzrZezc1hfZjBsW0+4j" +
                    "t/VNNuOHxTSsmSJzPM/d7a/vspm4Cc6sL7OZtBHOWBnkeep75Mz6OpspG+GMl0Eeu2aCDPI897WZ6xtt1taK0Wh66ytt1tUOk4qYLIM8P/7eocowFRnkee4O" +
                    "e32rzcaNEGNkkOe5Qcj6XpvNG+GMk0Ee2xkvkzy2M0EmeWxnokzy2M4kmeR56hFks77j5nytMxpblfUtN/cFHbjdUcUsERSB+3L0LCEUYfxOW6NissRQBO5G" +
                    "O0sQRaDiH0yWKIrATUOyhFGwx0jiKNi+SCAF2xeJpGD7IqEUgfrCSFOAShHO4wTnX/HWeMBSVDij0WEKcCkC90ReATBF4MaKBcgUgfpOKlMATRHI2ANgUwRu" +
                    "RFQAThGor+syBegUbGcAT8GtGdsBnyJQ33xnOwBUBOprPm0HhIrApWV0gKgI1IzTdsCoCNS3U9kOIBWBeiLEdkCpCNQ3t9sOMBWBi4bogFMRqBGR7QBUQXbG" +
                    "AKmC7IwBVAXbGWBVsJ0BWEUY//GFxtQ2QKsIXEqQAVxF4GKCjORVROqu3xoJrIjUXb81klgRuWhbI5EVkXpW0VrJrIjUVNFaCa0g14uV1AryPLISW8H2RXIr" +
                    "IvXVb9ZKcAXbFyBXRGqSZy2gK8jdzgK7InK3bhbgFZF6Js86oFewxQC+IlIjIuuAXxG5EZEDgAXbGSBYxPERkQbb1QHCInIjIgcMC3KfcQCxYDsDFAu2GMBY" +
                    "kKe2B45FHL91UyHfAsgicrGqHkgWkbvD9oCyiNzswQPLInIjIg8wi8iNiDzQLNjOAM6C7QzwLCqmtsY22wPQIlKxszYA0YLsTACkBbnPBMm0SA+0OMJkChJq" +
                    "0aul3VwKkmrRq6Xd511BYi3YYyS5Fr1a2iUzQYIt2L5IsgW7diXaguxLlGyLinmkUS9Rwi3IYxSBblFhjMaKFAFvUTGrNbpdBL6FEOMJeVUEwEWvmHbb/QiE" +
                    "C7YYQFz0imnYZYBxUVEzKmIAckF2JgHlosIZjSQvAeaiQoxGn0nAuegV064DJwBdVExthXOcNgHpoqKANTpwAtQFeWonYF0IMYGwUCaAXfSKabdHSUC76BXT" +
                    "btVOgLvoFdPsCp7NgLuocEajgDPgLiqcUREDuAu2M4C7IM+mDLgLtjOAu+gV0+7kWQbcRUUBq4gB3AV5OcgSd5HH7/pVtEjcRa+WdglnkbgLsi9F4i4yN4Eo" +
                    "EneRuSlRkbgLti8Sd8H2RfIu2L5I3gXbF8m7yNzwrADvomKQVJod8C4qRknBGdcB7yJTgxDXAe8iUze1rgPeRa8Y104M8C4ydYftOuBdsGsGeBdsZwB4UeGM" +
                    "QpLnOgBesJ0B4EWmfmLgOgBekKe2AeBFhTMKsaIzALzI1I8vnAHgRR6/3ddYKA0AL/L4Ta3KMAHwokKMSgED8CKPzx5UnAHgRaaGZ84A8CJTIyJngHjBdgaI" +
                    "F3l8KqNRMxaIF+TZZIF4Qa4ZC8SLCjEaHdgC8aKiZlSckcSLQj1j5awkXhTqrt9ZSbwo3L2tlcQLti+SeFGoKZGzknhB9sVJ4gXZFyeJF2xfJPGC7YskXrB9" +
                    "AeJFoSZ5zgHxolA/Q3EOiBdl/FkZlWEC4kWFGIVPdJwD4kXhBiEOiBflq4RnK+jdOWjxALwgG+MBeFG4qaIH4EVF/WoEeR6AF4WbEHkAXrBrBoAXZXxcpfG4" +
                    "6QF4UbipogfgBXs2AfCiUM9gOA/AC7KYAMCLwt3tBwBeFG5CFAB4wXYGgBdsZwB4wXYGgBdsZwB4wXYGgBeFesDVBQBeVDijIgaAFxUdWGPVjhJ4sdxWjj2n" +
                    "olEzURIv+sU0ewuFixJ50S+m3dSOknnRL6bd1I4SekF3RlIv6M5I7EVNAZ8D4v3eK39avHNdaJHYC7oxkntBLxkJvmA7k4B8wbYmAfqC7g2wL6Qaxm47Afyi" +
                    "X027T20T0C/61TSsG8BffDU1X5TrrX5vIC/66buLWzeEFqBf0J0B/EVN1ZzDmnD6zkeLG38SYgB/QbcG+Bc101sjZswAwGB7k4GAUeONRrPJgMCgewMMjBpv" +
                    "NHZQGSAYUg3jnF4GCka/mnZLZgYMRr+ahnUDHAy6NwDCoHsDJIx+NWpbl7sf/fjeM+8/EFOAhMG2pgAKg102BVgYdG8AhkH3BmgYNa1YRQ3gMGqmlEacVSQP" +
                    "Y7m/G3tkT2OVKhKIUSNGZZwkEaNfTLtD/0UiMfrFNJvdvpNMjH4xtp0YCcWgOyOpGHRnJBaD7ozkYtCdkWAMujNAxqCrATRGv5pm7z30HbAxpBrC8T3fARyj" +
                    "X02zgzXeAB2jX027KWUAj9Gvpl0VG+Bj1KhRiEm8AUAG3RsgZNTMKYV4zRtAZNC9AUYGfU4BJKPGG5V+A5SMmrpR2Ld4A5gMdt1Y4GRINYQdprcAyuhX49up" +
                    "AVJGv5p2c8oCKqNfTcO6AVYG3RuAZdC9AVoG3RvAZdC9AV4G3RsAZtT0G409gwNiRo03GuuUA2RGjRoVbyQzY/mQTLyg652EZvSLabebcpKa0S+m3YRyEptB" +
                    "FyO5Gf1i2jU+J8EZdGckOYPujERnsJ3xkp3BdsZLeEaNGI0+44GeUdP1NFqwB3wGfaCAnyHVEO6KeQ8AjX417dYnDwSNfjUN5zcgNOhqgKHRr6ZhFQNEg+1N" +
                    "AIpGjTcaSWgAjAbdG+Bo9Ktpl9oEAGnQvQGSRk33U1EDKA32DA/A0pBqGHu6ADCNfjUNqxhoGnRvAKdRo0bDmwg8jX417ao4AlCD7g0QNejeAFKD7g0wNdhz" +
                    "KgJUo6bfqKgBqka/GtdupACrQa8bydVY7mOIKFgfJVijX0yzU48+SbJGjTMaw5QkWoPujGRr1IhRcUbCNfrFtFsTkqRr9ItpdvPGJ4nXoDsj+Ro1NaOxICQJ" +
                    "2KgZJhUxQNioUaMytwGxwW57GRgbUg3jPEkGyEaNGo20LwNlo19Nu+eIDJiNfjXtuk0GzkaNGo1GnAG00a/GtVMDpI2aKtY425cBtUGvYmBt1KhRmeEA26gZ" +
                    "KY11qgBtgz3DC+A2akZKxRvgbUg1hNe9+wLAjRo1Kt4AcaNfTbteXAC5UaNGpYqBuVEzUipqALpB9waoG3RvALtB9iZ0wN2oUaPQb0IH4I1+NaGdN0DeoKsB" +
                    "9Ea/mmYvTQ2dZG8sdw6fiontF4bQSfhGvxjXToykb9SIURkmid/oFxPazW7J36APkwRw1BSwxtw2ksDRL6bdkmAkgoPujGRwsGvGSAgHe2oboHDUzG2VcQIM" +
                    "R78a304NcDikmsQoGwBx9KtpOL2BxFHjjcIuKhhAcfSrafZ5VLAA42CPlAUaR7+adp3YAo6jxhuFtC9Y4HHUeKMxwy0AOeh1A0SOmjmlogaQHHRvgMnRr6bd" +
                    "qmkByiHVZIY3QOXoV9Pso9XgAMvB9sYBl6NfTbte7ADMQfcGyBx0bwDNQfcG2Bx0bwDOQfcG6Bw1/UZFDeA5akZK40nUAZ+jX02zk4bBA6AjjN/caVjjAdAR" +
                    "uPteD4COwI0EPAA6KsRopH0eAB2BGyN5AHQEbsDmAdDBdgYIHWxngNDBdgYIHRV9RuNpOCChg2xNQEJHRafRyJACEjoCd4cZkNARuLvvgISOwM3XAhI62COF" +
                    "jI7AzdcCMjrY3iCkg+0NQjoqqlgje4wI6ajwRqP7RYR0kOsmIqSjQo3GM2hESEfFSKmoQUhHGL/7VlGDkI7AzQIiQjoCNyeJCOlge4OQDrY3COkI55FMVEA6" +
                    "/uf4w2cfiEkI6SBbkxDSQS6bhJAOtjcI6WB7g5AOthqEdFSo0XhITwDpiNx9bwJIR4UYFWcA0hG5yWMCSEfkBgIZIB2RmyJlgHTE8Wm1xuN5BkgH2xmAdLBr" +
                    "BiAdbGcA0sF2BiEdFdZodL2MkA7ygpAR0hHHRwEqahDSEccfptMYqYKQjsiNAgpCOiI3QioI6WB7g5AOtjcI6ahQoxGvFYR0sL1BSAe7bhDSwfYGIR1sbxDS" +
                    "Eccf7VOI12KHkI5I3X3HDiEdkbrfjR1COthqENLBHimEdLC9QUgH2xuEdFSoUdjZxQ4hHWxvENLBrhuEdETqUeJoENJBHimDkI5IPdoXDUA60vitpsaiaQDS" +
                    "kcbvwhV2U9EApCNRI4FoANKRqGFJNADpqBCj0YYNQDrYzgCkI40/F3UOn43de+bjxa0bQgswOsjGWGB0kCeTBUYH2xlkdLCtQUZHop4hjhYZHYm63Y0WGR2J" +
                    "ut2NFhkdbG+Q0cH2BhkdbG+Q0ZHO40To+FXhF9dOXvnFAzEOER1kaxwiOshl4xDRwfYGER1sbxDRUeGNxrO5Q0RHGp/1aewvHSI6End/6RDRkcYnARqP5w4R" +
                    "HULN8hmjb6isauEgo6NfjumVozFWHiEd/XJsOzlI6eiX43rlaNSOR0wH3R3kdNBrB0EddHeQ1FFTOxpbB4+ojpq+oyIHWR3sNugB1pHH74A1lk8PsI5MfXVU" +
                    "DADryNQ35sUAsI7MjQYCwDoyNzUJAOtgOwOwDrYzAOtgOwOwDrYzAOtgO4Owjkx9p0IMCOvI1DN+MSKsI48/VacxUhFhHZl6iSxGhHVkbioQEdaRuYlJRFgH" +
                    "2xuEdbC9QVgH2xuEdeTxkajGPjMirIPtDcI6MvUKbUwI68jUE84xIayDrQZhHXn8KReVkUJYR6ZeL44JYR2ZHJYkpHVkcliSENdBdwd5HXR3ENhBdweJHWx3" +
                    "MiI72O5kZHbQ3UFoR407GluZjNSOfjm+nTuA7Sjc3C8DtqNQ7/bGDNiOwg0HMmA7CjcRzYDtYDsD2I7CDZQKYDvIzhTAdrCdAWwH2xnAdpTzOJT5V4hBbEfh" +
                    "fsBQENtRqHyVWBDbUah3e2NBbEfh3icriO0o3BMvBbEdhZqYpA6xHYWatKUOsR1sbxDbwfYGsR1sbxDbUaiv6kwdYju4Mzx1iO2o8Eah+6UOsR2FmommDrEd" +
                    "pS9pC63S69QhtqNCjYY3BrEdhRsHJIPcjsINS5JBcAfdHSR30N1BdEc5l7Bk9En50zefXfzhfaEG0R10c5DdQS8dhHfQ3UF6B90dxHfUyNFoylbyO5ZfEC/5" +
                    "Jiv5Hf1imr1FL1nJ7+gX49qJkfyOfjHtHrms5HfUiNF4xrGS39Evpt0mxkp+R81sUhEj+R10ZyS/o2Y2KST6yUp+B7vPOOB3sMfJAb+jxhuNraYDfodUQ4j9" +
                    "kgN+R7+aht4Av6NfTbsFygG/o8YbFTXA7+hX064RO+B39KtpOMOB30H3BvgdbG888DtqqljjOcsDv4PdbzzwO2p6scpIAb+jpvupjBTwO6Qawiu2kgd+R7+a" +
                    "hnMK+B1SDeO6XfIA8BiQ0y4O8EDwGJDTLizxgPCguxMA4kF3JwDFg+8OYDz47gDHg+8OgDyq+o7G8hmA5DEgx7dzR6I8lk8Yn6op/ZVjVL2RKI9+Ma6dGIny" +
                    "6Bfje8VoJBVBojz6xYRmzkSJ8mDXTJQojxoxGrMpSpRHvxjbToxEedCdkSiPmgLWePaLEuVBn02A8qhRo9FoIqA86N4AyoOuBlAeUs1y8L5wfmtUcQKWx4Cc" +
                    "do04AcxjQI5r5w7QPAbk+HbuAM6jarA02nECnkdVKavIAaAH3x0gevDdAaTHgJx2bTAB04MvB6Ae9DaYgepRJUejDWbAelQNlooc4HqAHMLdu5QB7DEgp90h" +
                    "nAxkjyo5Gn0nA9qjarBU5ADbg+8OwD347gDdg+8O4D2q5GhsawrwPQbk+HZyAPAxIKfZmxBTkYSP5bo+NurS6MlFEj76xbQLAYskfPSLafdoUSThgy5GEj5q" +
                    "xGh0myIJH/1i2j2uF0n4oDsjER9kZ3InER9kZ3InER/k2ZQ7QHzUtGCF9SB3gPio6cEKi2XuAPFR442KGkB8SDVfmg2oFA4wPgbkhHZyAPIxIKfhtALKB3+w" +
                    "APNRJUejAxoAfVQNloocIH1UlbLChYJsAPXBdwdYH/zaAdjHgJxmz+nZAO2DPtEN4D7oXdkA74MvB4AfIMe2PwKTDQA/quRoPHxZAH4MyGmWK2ULwI8BObad" +
                    "HAB+8N0B4AffHQB+8N0B4MeAnNBuZgHwo2qwNNqgBeDHgBzXKpPMVgI/lnVNzN2ylcCPfjHtVnMngR/9Ynw7MRL40S+m3UOpk8CPmmFSESOBH3RnJPCD7owE" +
                    "ftCdkcAPujMS+EF3BoAfNS1YYz1wAPzoV9Nuz+AB+MFuwh6AH1INZbvpgfgxIKfZ0bvsAfkxIKfZkdbsgflRJUcjV/IA/RiQ067peKB+VJWyihzAfvDdAe4H" +
                    "3x0Af9DdCUD+qJKjMbMCoD+qJrpGVw7A/qhyR0UOwD8G5LRbQgPQP0DOl+1+VUoZ6B8DctolJwHoHwNy2uVKAegffHeA/sF3B+gfdHci0D/o7kSgf/DdAfpH" +
                    "lRyNNhiB/sEfLEn/WK5czD1flPSPfjHtzsRESf/oF9PuwStK+ke/mHYPpVHSP+jOSPoH25kk6R81BXwO76pdPP+zxcdv3/uXX5+++L9CkUSA0O2RCBB24SSJ" +
                    "AKE7AwgQujWAAGGvCgkQIDUjpaIGECBSzZdGbxpb4AQMkCo5GtFbAgjIgJx20yoDBWRATrt5lQEDwncHOCB8dwAEUlXK57B83v34pbsfvX364pswvTLgQPgW" +
                    "AQ+EX0AABOG7A0QQvjuABBmQ024/k4EJUuWOxjpaAAoCchgpQQEoyICcdtvyAlAQvjsABeG7A1CQATlOCwqyuH37+PWfLX75q8Xvfi00ARqEbxGgQfgFBGgQ" +
                    "vjuABuG7A2iQKjkKrbl0gAYJ1IOBpQM0SKDe2isdoEHYYgANEqiRQekADRKoaUrpAA3CdgbQIGxnAA3CdgbQIGxnAA1CdsYgGiRQA7hiEA1S4Y1C4lUMokEC" +
                    "9c2SxSAaJHAPWRSDbJDAvaVXDMJB6HKQDhK4AUExiAcJ3HOKxSAfhO4OAkICN1wqFgkhbHcsIkLo7iAjhO4OQkIC9yResUgJoctBTEjgJoHFIickVKQ5KnKQ" +
                    "ExK42UCxyAkJ3OSkWOSEsN1xyAlhu+OQE0J3BzkhdHeQE0J3BzkhoeKyssJH58UhJyRUvJVARQ5wQiL1+lVxwAmJ1BMgxQEnhO0McELIYjxwQiI3Q/HACYnc" +
                    "dMkDJ4TtDHBC2M4AJ4TtDHBCIveTD4+ckEg9Dl08ckIiN5L0yAkhrwgeOSGRvKEKyAmJ5EwyICckkkPAgJyQyL00WAJyQiI5OQnICYnkXCkgJ4TuDnJC6O4g" +
                    "JyRyzyKXgJyQyH0rQQnICYnk1C0iJ4TddyJyQmpmlsZ2MyInJJJDwIickEjejEfkhERychKRExLJuVJETgjdHeSE0N1BTgjdHeSEsN1JyAmhy0FOSCSdBHxo" +
                    "+d0HO4eTrW8+ObkyX/30B2bMnnh8djCf7V2arYHG23uPnvmxM3t4vjc/OhM8eeTK9t7lh698+3B/7/Jk+e3bB3uzna8/Ot/dnWxNzv7OZz/afoUfvb337f2H" +
                    "H55fmh180U92X+0nH17av3LU93Mfmk4e3j94bHv1Z79ppnbqHsI/t7TsoeWoPb5/cLTy72h+tPtgrC/834cXjl/+ZPHCG8fXXlz86NZkOjmaPXG0/P3X3jl+" +
                    "44f377x6evu3iw9fv3/nmgnTcv/O9W/tHV97afHca/fvvOpDd+H/n/vJBW/ThQuLd66tftW41a9239r71t7JW2+ZxWuvL168dv/Oq4sffnLv6tULxnX37zx/" +
                    "/MKtxS9vX7Bu+ccWH753euPDxR9/du8/nlvcfPf45d+fvPajux/dOH77vxbvPnfyyb/e++/n/3z1mcl08r3lX3Z1OvpoviwTZ+zFbmnGY7PDw+3Ls+Vfc/hv" +
                    "d/q7V05u/Ob0gzfuXX36guu6CydPf3By4/r9O68d33zr5Pq145tvHb/378c3ry9+8IPF1TuLnzx/cvPW4p3Xj1/+3d1Pfr6ScPLbH5/+/v2VL3+++sziB9cW" +
                    "L7yxFP7Sr0//+Ie7H109+fhXpx88e/zxi4ub796/8/ziuReOr/9mcfuVk58+t7h1a/lDHppO9g92ZgfrMdk+uDw7Wrahshysx7bne/O9y8t442KZTnauHKzH" +
                    "ePk3Xf6BoysHqz+w9OGph576C6OGF3SHHQIA";
                break;
            case "returned":
                expectedSha = "9A40C323134C349B4C6E2D681E7B4B2A360528DF97F79CBAC2DCBB27DB123825";
                encoded =
                    "H4sIAAAAAAACCs2dXXNdxZmF/4rrXKtcuz93t27nZuZuKpnMXCRUSrGOjQYhUZJMqFBUGQLETsCBDCQkuDCZCQOZ8GUIwQFPqJqfMvHRx5X/QuroHPC7it6w" +
                    "m+jtdbigkG2J5dVvv717ne79PD7Z33h0+q/Tvf2t3Z3JulmbPLzxyD9tTtYnk9P//LetzYMHJ+vd6Rf/ON269ODB5199a/ro1uK7urXJ3uXt6d5kffLt6d7W" +
                    "xvbWjzYOtnYna5Pdva1LW4s/8YOtvYMHvzW9dPodfm3y4O7D0/n/yPVhbXKwt7v7yP5k3dt+bbIzfezgHzb2Nue/69cmG9vb/7y7u73/nZ3t3QsPTTcn6xc3" +
                    "tvena5Pp9sYj+/OvnfXnu7XTv8nmdw4u/MvWhYf2F6qm+7uX9y5M9yfrj08u7W3s70/WXdeZ87G7/49fm//WXKXvOns+i3/C2mT/YHdnOlk3ycTzdm2ytTeX" +
                    "n7t03qxNLu1ub07WTfThfBI/0K5N/n1jc3r6v3pibbK9sbO5P1n/7uOTrc1TVY+d/vtHS4WXPrdwe/rodHuyHhbWzL/r9Jcf2pr/h1ub7P5wR/zlL2w8snWw" +
                    "sf3F1/Pv+eKLH+xtbV66/+XOxvz3Jif/8eTsxieTJ9YWSsypElOrpDsDJUcfvHv00vtfKLGnSuywEttKiTtV4oaV+FZK/KkSP6zEFJXYs1By80NZJ+FUSViB" +
                    "0YmnSmKtJwpK+lMl/Qp4kk6VpBXwJJ8qycNKXFGJOQMlx89fg862aLKmW4HeZpZt1qxArZhFozVf6rTma3xR6Cpm0WqNG9bSrNeaRbM1flhLuzFatFsTasdI" +
                    "Q8ui4Zo4rKVZdzGLlmv6VRijRdM1aRV8WbRdk4e1lPuuPwMthx98ePjRk/ef4xZ913Yr0F/sou9aUztGCuuRXT7gVvddhad+u+i71tXOIw0ti75rv9R37dfU" +
                    "rsYeZNF3bRjW4pvV7qLv2jispd3ebNF3bb8KWhZ916ZhLc36rl30XZuHtbTbtS76rutWwBe36LvOrMCcdou+6+wK1K5bRguu1heFvusWfdf5VaiXRd91odYX" +
                    "hXXaRRHOGW4Q1Yt0zozfTSusRi6JeM6M30xrDFAW+RxXiu9EQMetFW9EQmeoGZC3IqIju+JERkd2xYuQjuxKECkdWUqUKR23x/lepnTkakkypTPjd4saXS7L" +
                    "lM5Q06jQyZTOjE86NLQYmdIZagIUrEzpDDUZC06mdGRfvEzpzPiEWSEZC0GmdGRfokzpKrRofLbYy5SuYk4r9LqQZEpH7i9ZpnQVWsIZaJndujW7IT517WRK" +
                    "Z6g7+mhkSmfGpy4avliZ0hnqLjo6mdKRtXiZ0hlqMhaDTOnM+FRXo16iTOnIvvQypSPXS5IpXUV/UViPYpYpXYUvClr6TqZ03L7bG5nSGWqS2luR0tnxW1iN" +
                    "IXIipbPUHWzvRUpnxx9dU4iX+yBSOkvNGPooUjrLPTLWi5SO7EoSKR3ZlSxSOq4rqRMpHdeVZERKR3bFypTOUs/1JSdTOm7rT16mdJaajKUgUzo7freo4UuU" +
                    "KZ2lJh2plymdpSZjKcmUjuxLlikd15fcyZSuQotCSpeNTOnIvliZ0nHrJTuZ0lX4otBfspcpHbfX5SBTOu4akKNM6ez4HZrCOp17mdLZ8UmHhpYkUzpLTTpy" +
                    "limdpSZAputkTMc1xnRG5nRsZ6wM6tjOOJnUsZ3xMqpjOxNkVmep+Zjpogzr2MPUy7SO239Nl0Rc57g3Xrss8jo3fi+roWV5Sc0Oa2lXMMtLam5YS7tpvbyk" +
                    "5lfCFycyO7YvXoR2FbU7rOVg7/LIbdKta3dvvyOkBBHasW2JIrVjl0svYju2L0nmdmxjsgzuyGKWt9TMlzqvaf9RiVleUzNuWEy7mlneUzN+WEzDYXIyvGM7" +
                    "42V6x3YmyPjOncXV6dFLwfEbT5+88qHQEmV856iH7MzyrprJK1EySQZ47JLJMsEjO7O8rmbtKjizvK9mv6IBN3wngpUhnuPuIpc31mwYFtOwZryM8djOBJnj" +
                    "sZ2JMsdjO9PLHM+dRSYzemk6evO/Tn79kdCSZIzHNibLGI9cMsu7a86ugjPL22vOrYQzVsZ4FWI0krPlDTb3FQ24XXS2vMO2mEyeu4NbXmIzw1oavjooihiv" +
                    "QovG48PyFptbiTFKIsar0KJw8Nosb7GFVfBleYstDmtp1+2Wt9j6lfDFihzPj483NepleYstr4QvXuZ47IIJMsfz3A9QlvfYzJdaL2PntrzIZtywmHYfuS1v" +
                    "shk/LKZhzWSZ43nubn95l83EVXBmeZnN9CvhjJVBnqe+R84sr7OZvBLOeBnksWsmyCDPc1+bubzRZm2tGI2mt7zSZl3tMKmISTLI8+PvHaoMU5ZBnufusJe3" +
                    "2mxcCTFGBnmeG4Qs77XZtBLOOBnksZ3xMsljOxNkksd2Jsokj+1ML5M8Tz2CbJZ33JyvdUZjq7K85ea+ogO3O6qYJIIicF+OniSEIozfaWtUTJIYisDdaCcJ" +
                    "oghU/INJEkURuGlIkjAK9hhJHAXbFwmkYPsikRRsXySUIlBfGGkyUCnCWZzg/DveGg9YigpnNDpMBi5F4J7IywCmCNxYMQOZIlDfSWUyoCkCGXsAbIrAjYgy" +
                    "wCkC9XVdJgOdgu0M4Cm4NWM74FME6pvvbAeAikB9zaftgFARuLSMDhAVgZpx2g4YFYH6dirbAaQiUE+E2A4oFYH65nbbAaYicNEQHXAqAjUish2AKsjOGCBV" +
                    "kJ0xgKpgOwOsCrYzAKsI4z++0JjaBmgVgUsJMoCrCFxMkJG8ikjd9VsjgRWRuuu3RhIrIhdtaySyIlLPKlormRWRmipaK6EV5HqxklpBnkdWYivYvkhuRaS+" +
                    "+s1aCa5g+wLkikhN8qwFdAW521lgV0Tu1s0CvCJSz+RZB/QKthjAV0RqRGQd8CsiNyJyALBgOwMEizg+ItJguzpAWERuROSAYUHuMw4gFmxngGLBFgMYC/LU" +
                    "9sCxiOO3birkWwBZRC5W1QPJInJ32B5QFpGbPXhgWURuROQBZhG5EZEHmgXbGcBZsJ0BnkXF1NbYZnsAWkQqdtYGIFqQnQmAtCD3mSCZFv19LY4wmYKEWhS1" +
                    "tJtLQVItilrafd4VJNaCPUaSa1HU0i6ZCRJswfZFki3YtSvRFmRfomRbVMwjjXqJEm5BHqMIdIsKYzRWpAh4i4pZrdHtIvAthBhPyKsiAC6KYtpt9yMQLthi" +
                    "AHFRFNOwywDjoqJmVMQA5ILsTA+UiwpnNJK8HjAXFWI0+kwPnIuimHYduAfQRcXUVjjHaXsgXVQUsEYH7gF1QZ7aPbAuhJhAWCh7gF0UxbTbo/RAuyiKabdq" +
                    "94C7KIppdgXPJsBdVDijUcAJcBcVzqiIAdwF2xnAXZBnUwLcBdsZwF0UxbQ7eZYAd1FRwCpiAHdBXg6SxF2k8bt+FS0Sd1HU0i7hzBJ3QfYlS9xF4iYQWeIu" +
                    "EjclyhJ3wfZF4i7YvkjeBdsXybtg+yJ5F4kbnmXgXVQMkkqzA95FxSgpOOM64F0kahDiOuBdJOqm1nXAuyiKce3EAO8iUXfYrgPeBbtmgHfBdgaAFxXOKCR5" +
                    "rgPgBdsZAF4k6icGrgPgBXlqGwBeVDijECs6A8CLRP34whkAXqTx232NhdIA8CKN39SqDBMALyrEqBQwAC/S+OxBxRkAXiRqeOYMAC8SNSJyBogXbGeAeJHG" +
                    "pzIaNWOBeEGeTRaIF+SasUC8qBCj0YEtEC8qakbFGUm8yNQzVs5K4kWm7vqdlcSLzN3bWkm8YPsiiReZmhI5K4kXZF+cJF6QfXGSeMH2RRIv2L5I4gXbFyBe" +
                    "ZGqS5xwQLzL1MxTngHiRx5+VURkmIF5UiFH4RMc5IF5kbhDigHiRv0l4toDenYEWD8ALsjEegBeZmyp6AF5U1K9GkOcBeJG5CZEH4AW7ZgB4kcfHVRqPmx6A" +
                    "F5mbKnoAXrBnEwAvMvUMhvMAvCCLCQC8yNzdfgDgReYmRAGAF2xnAHjBdgaAF2xnAHjBdgaAF2xnAHiRqQdcXQDgRYUzKmIAeFHRgTVW7SiBF/Nt5dhzKho1" +
                    "EyXxoiym2VsoXJTIi7KYdlM7SuZFWUy7qR0l9ILujKRe0J2R2IuaAj4DxPvJy5/N3r0mtEjsBd0Yyb2gl4wEX7Cd6YF8wbamB/QF3RtgX0g1jN12D/CLspp2" +
                    "n9r2QL8oq2lYN4C/+GZqvirXW/zeQF70y/dnN68LLUC/oDsD+IuaqjmDNeH43duz658JMYC/oFsD/Iua6a0RMyYAYLC9SUDAqPFGo9kkQGDQvQEGRo03Gjuo" +
                    "BBAMqYZxTi8BBaOspt2SmQCDUVbTsG6Ag0H3BkAYdG+AhFFWo7Z1uXv7ZydPfXhfTAYSBtuaDCgMdtlkYGHQvQEYBt0boGHUtGIVNYDDqJlSGnFWljyM+f5u" +
                    "7JE9jVUqSyBGjRiVcZJEjLKYdof+s0RilMU0m92+k0yMshjbToyEYtCdkVQMujMSi0F3RnIx6M5IMAbdGSBj0NUAGqOsptl7D30HbAyphnB8z3cAxyiraXaw" +
                    "xhugY5TVtJtSBvAYZTXtqtgAH6NGjUJM4g0AMujeACGjZk4pxGveACKD7g0wMuhzCiAZNd6o9BugZNTUjcK+xRvAZLDrxgInQ6oh7DC9BVBGWY1vpwZIGWU1" +
                    "7eaUBVRGWU3DugFWBt0bgGXQvQFaBt0bwGXQvQFeBt0bAGbU9BuNPYMDYkaNNxrrlANkRo0aFW8kM2P+kEy8oOudhGaUxbTbTTlJzSiLaTehnMRm0MVIbkZZ" +
                    "TLvG5yQ4g+6MJGfQnZHoDLYzXrIz2M54Cc+oEaPRZzzQM2q6nkYL9oDPoA8U8DOkGsJdMe8BoFFW02598kDQKKtpOL8BoUFXAwyNspqGVQwQDbY3ASgaNd5o" +
                    "JKEBMBp0b4CjUVbTLrUJANKgewMkjZrup6IGUBrsGR6ApSHVMPZ0AWAaZTUNqxhoGnRvAKdRo0bDmwg8jbKadlUcAahB9waIGnRvAKlB9waYGuw5FQGqUdNv" +
                    "VNQAVaOsxrUbKcBq0OtGcjXm+xgiCtZHCdYoi2l26tH3kqxR44zGMPUSrUF3RrI1asSoOCPhGmUx7daEXtI1ymKa3bzxvcRr0J2RfI2amtFYEHoJ2KgZJhUx" +
                    "QNioUaMytwGxwW57CRgbUg3jPEkCyEaNGo20LwFlo6ym3XNEAsxGWU27bpOAs1GjRqMRJwBtlNW4dmqAtFFTxRpn+xKgNuhVDKyNGjUqMxxgGzUjpbFOZaBt" +
                    "sGd4BtxGzUipeAO8DamG8Lp3nwG4UaNGxRsgbpTVtOvFGZAbNWpUqhiYGzUjpaIGoBt0b4C6QfcGsBtkb0IH3I0aNQr9JnQA3iirCe28AfIGXQ2gN8pqmr00" +
                    "NXSSvTHfOXwuJrZfGEIn4RtlMa6dGEnfqBGjMkwSv1EWE9rNbsnfoA+TBHDUFLDG3DaSwFEW025JMBLBQXdGMjjYNWMkhIM9tQ1QOGrmtso4AYajrMa3UwMc" +
                    "DqmmZ5QNgDjKahpObyBx1HijsIsKBlAcZTXNPo8KFmAc7JGyQOMoq2nXiS3gOGq8UUj7ggUeR403GjPcApCDXjdA5KiZUypqAMlB9waYHGU17VZNC1AOqSYx" +
                    "vAEqR1lNs49WgwMsB9sbB1yOspp2vdgBmIPuDZA56N4AmoPuDbA56N4AnIPuDdA5avqNihrAc9SMlMaTqAM+R1lNs5OGwQOgI4zf3GlY4wHQEbj7Xg+AjsCN" +
                    "BDwAOirEaKR9HgAdgRsjeQB0BG7A5gHQwXYGCB1sZ4DQwXYGCB0VfUbjaTggoYNsTUBCR0Wn0ciQAhI6AneHGZDQEbi774CEjsDN1wISOtgjhYyOwM3XAjI6" +
                    "2N4gpIPtDUI6KqpYI3uMCOmo8Eaj+0WEdJDrJiKko0KNxjNoREhHxUipqEFIRxi/+1ZRg5COwM0CIkI6AjcniQjpYHuDkA62NwjpCGeRTFRAOv54+PHT98X0" +
                    "COkgW9MjpINcNj1COtjeIKSD7Q1COthqENJRoUbjIb0HSEfk7nt7gHRUiFFxBiAdkZs89gDpiNxAIAGkI3JTpASQjjg+rdZ4PE8A6WA7A5AOds0ApIPtDEA6" +
                    "2M4gpKPCGo2ulxDSQV4QEkI64vgoQEUNQjri+MN0GiOVEdIRuVFARkhH5EZIGSEdbG8Q0sH2BiEdFWo04rWMkA62NwjpYNcNQjrY3iCkg+0NQjri+KN9CvFa" +
                    "7BDSEam779ghpCNS97uxQ0gHWw1COtgjhZAOtjcI6WB7g5COCjUKO7vYIaSD7Q1COth1g5COSD1KHA1COsgjZRDSEalH+6IBSEc/fqupsWgagHT043fhCrup" +
                    "aADS0VMjgWgA0tFTw5JoANJRIUajDRuAdLCdAUhHP/5c1Bl8Nnby1Cezm9eFFmB0kI2xwOggTyYLjA62M8joYFuDjI6eeoY4WmR09NTtbrTI6Oip291okdHB" +
                    "9gYZHWxvkNHB9gYZHf1ZnAgdvyr8+urRy7++L8YhooNsjUNEB7lsHCI62N4gooPtDSI6KrzReDZ3iOjox2d9GvtLh4iOnru/dIjo6McnARqP5w4RHULN/Bmj" +
                    "NFRWtXCQ0VGWY4pyNMbKI6SjLMe2k4OUjrIcV5SjUTseMR10d5DTQa8dBHXQ3UFSR03taGwdPKI6avqOihxkdbDboAdYRxq/A9ZYPj3AOhL11VExAKwjUd+Y" +
                    "FwPAOhI3GggA60jc1CQArIPtDMA62M4ArIPtDMA62M4ArIPtDMI6EvWdCjEgrCNRz/jFiLCONP5UncZIRYR1JOolshgR1pG4qUBEWEfiJiYRYR1sbxDWwfYG" +
                    "YR1sbxDWkcZHohr7zIiwDrY3COtI1Cu0sUdYR6KecI49wjrYahDWkcafclEZKYR1JOr14tgjrCORw5IeaR2JHJb0iOugu4O8Dro7COygu4PEDrY7CZEdbHcS" +
                    "Mjvo7iC0o8Ydja1MQmpHWY5v5w5gOzI390uA7cjUu70xAbYjc8OBBNiOzE1EE2A72M4AtiNzA6UM2A6yMxmwHWxnANvBdgawHfksDmX+HWIQ25G5HzBkxHZk" +
                    "Kl8lZsR2ZOrd3pgR25G598kyYjsy98RLRmxHpiYmfYfYjkxN2voOsR1sbxDbwfYGsR1sbxDbkamv6uw7xHZwZ3jfIbajwhuF7td3iO3I1Ey07xDbkUtJW2iV" +
                    "XvcdYjsq1Gh4YxDbkblxQG+Q25G5YUlvENxBdwfJHXR3EN2RzyQsGX1S/viNp2d//lCoQXQH3Rxkd9BLB+EddHeQ3kF3B/EdNXI0mrKV/I75F8RLvr2V/I6y" +
                    "mGZv0eut5HeUxbh2YiS/oyym3SOXlfyOGjEazzhW8jvKYtptYqzkd9TMJhUxkt9Bd0byO2pmk0Ki31vJ72D3GQf8DvY4OeB31HijsdV0wO+QagixX++A31FW" +
                    "09Ab4HeU1bRboBzwO2q8UVED/I6ymnaN2AG/o6ym4QwHfgfdG+B3sL3xwO+oqWKN5ywP/A52v/HA76jpxSojBfyOmu6nMlLA75BqCK/Y6j3wO8pqGs4p4HdI" +
                    "NYzrdr0HgMeAnHZxgAeCx4CcdmGJB4QH3Z0AEA+6OwEoHnx3AOPBdwc4Hnx3AORR1Xc0ls8AJI8BOb6dOxLlMX/C+FxNLleOUfVGojzKYlw7MRLlURbji2I0" +
                    "koogUR5lMaGZM1GiPNg1EyXKo0aMxmyKEuVRFmPbiZEoD7ozEuVRU8Aaz35RojzoswlQHjVqNBpNBJQH3RtAedDVAMpDqpkP3lfOb40q7oHlMSCnXSPuAeYx" +
                    "IMe1cwdoHgNyfDt3AOdRNVga7bgHnkdVKavIAaAH3x0gevDdAaTHgJx2bbAHpgdfDkA96G0wAdWjSo5GG0yA9agaLBU5wPUAOYS7d30CsMeAnHaHcBKQPark" +
                    "aPSdBGiPqsFSkQNsD747APfguwN0D747gPeokqOxrcnA9xiQ49vJAcDHgJxmb0LssyR8zNf1sVGXRk/OkvBRFtMuBMyS8FEW0+7RIkvCB12MJHzUiNHoNlkS" +
                    "Pspi2j2uZ0n4oDsjER9kZ1InER9kZ1InER/k2ZQ6QHzUtGCF9SB1gPio6cEKi2XqAPFR442KGkB8SDVfmw2oFA4wPgbkhHZyAPIxIKfhtALKB3+wAPNRJUej" +
                    "AxoAfVQNloocIH1UlbLChYJkAPXBdwdYH/zaAdjHgJxmz+nJAO2DPtEN4D7oXdkA74MvB4AfIMe2PwKTDAA/quRoPHxZAH4MyGmWKyULwI8BObadHAB+8N0B" +
                    "4AffHQB+8N0B4MeAnNBuZgHwo2qwNNqgBeDHgBzXKpNMVgI/5nVNzN2SlcCPsph2q7mTwI+yGN9OjAR+lMW0eyh1EvhRM0wqYiTwg+6MBH7QnZHAD7ozEvhB" +
                    "d0YCP+jOAPCjpgVrrAcOgB9lNe32DB6AH+wm7AH4IdVQtpseiB8DcpodvUsekB8DcpodaU0emB9VcjRyJQ/QjwE57ZqOB+pHVSmryAHsB98d4H7w3QHwB92d" +
                    "AOSPKjkaMysA+qNqomt05QDsjyp3VOQA/GNATrslNAD9A+R83e5XpZSB/jEgp11yEoD+MSCnXa4UgP7BdwfoH3x3gP5BdycC/YPuTgT6B98doH9UydFogxHo" +
                    "H/zBkvSP+crF3PNFSf8oi2l3JiZK+kdZTLsHryjpH2Ux7R5Ko6R/0J2R9A+2M72kf9QU8Bm8q3b23K9mn7xz8uO3jl/4X6FIIkDo9kgECLtweokAoTsDCBC6" +
                    "NYAAYa8KPSBAakZKRQ0gQKSar43eNLbAPTBAquRoRG89QEAG5LSbVgkoIANy2s2rBBgQvjvAAeG7AyCQqlI+g+Xz7icv3r39zvELb8D0SoAD4VsEPBB+AQEQ" +
                    "hO8OEEH47gASZEBOu/1MAiZIlTsa62gGKAjIYaQEGaAgA3LabcszQEH47gAUhO8OQEEG5DgtKMjs1q3D1341+92bs/feEpoADcK3CNAg/AICNAjfHUCD8N0B" +
                    "NEiVHIXWnDtAgwTqwcDcARokUG/t5Q7QIGwxgAYJ1Mggd4AGCdQ0JXeABmE7A2gQtjOABmE7A2gQtjOABiE7YxANEqgBXDaIBqnwRiHxygbRIIH6ZslsEA0S" +
                    "uIcsskE2SODe0ssG4SB0OUgHCdyAIBvEgwTuOcVskA9CdwcBIYEbLmWLhBC2OxYRIXR3kBFCdwchIYF7Ei9bpITQ5SAmJHCTwGyRExIq0hwVOcgJCdxsIFvk" +
                    "hARucpItckLY7jjkhLDdccgJobuDnBC6O8gJobuDnJBQcVlZ4aPz7JATEireSqAiBzghkXr9KjvghETqCZDsgBPCdgY4IWQxHjghkZuheOCERG665IETwnYG" +
                    "OCFsZ4ATwnYGOCGR+8mHR05IpB6Hzh45IZEbSXrkhJBXBI+ckEjeUAXkhERyJhmQExLJIWBATkjkXhrMATkhkZycBOSERHKuFJATQncHOSF0d5ATErlnkXNA" +
                    "TkjkvpUgB+SERHLqFpETwu47ETkhNTNLY7sZkRMSySFgRE5IJG/GI3JCIjk5icgJieRcKSInhO4OckLo7iAnhO4OckLY7vTICaHLQU5IJJ0EfGD+3Xub+5P1" +
                    "7z4+uby1+On3zZg+9sh0b2u6c2G6BBpv7Dx06sfm9OLWztbBqeDJg5c3di5dvPz9/d2dS5P5t2/s7Uw3v/3Q1vb2ZH1y+nc+/dH2G/zojZ3v7168uHVhuvdV" +
                    "P9l9s5+8f2H38kHp5z6wNrm4u/fwxuLPftes2TX3AP65uWUPzEftkd29g4V/B1sH2/fH+tz/fXzu8KVPZ8+/fnj1hdlPb07WJgfTxw7mv//qu4ev/+Tend8c" +
                    "3/rD7OPX7t25asJavnfn2vd2Dq++OHv21Xt3fuNDd+7/n/3FOW/7c+dm715d/Kpxi1/tvrfzvZ2jt982s1dfm71w9d6d38x+8unJlSvnjOvu3Xnu8Pmbs9/d" +
                    "Omfd/I/NPv7g+PrHs7/86uQ/n53deP/wpT8dvfrTu7evH77z37P3nz369Ocn//PcX688NVmb/HD+l12cjj7YmpeJM/Z8Nzfj4en+/sal6fyvOTn58Vsnr9yc" +
                    "/9TPXpq9+trd25/Obl6/d+e52c/fu/vpG8e/fWP2zEd/vfLk8fs/Pnz5j0d3fnnyys3Fjx+05fi9l4+u//74o9dPrjx5znXduaMnPzq6fu3enVcPb7x9dO3q" +
                    "4Y23Dz/47eGNa7NnnplduTP7xXNHN27O3n3t8KX37n76yvKH/+Fnx3/6cGHoX688NXvm6uz51+d/4xffOv7Ln+/evnL0yZvHHz19+MkLsxvvz9U++/zhtd/P" +
                    "br189MtnZzdPFT6wNtnd25zunY7sE38DRCeGYHwdAgA=";
                break;
            default: throw new ArgumentOutOfRangeException("state", state, "Unknown frozen legacy fixture.");
        }
        byte[] payload;
        using (var packed = new MemoryStream(Convert.FromBase64String(encoded)))
        using (var unzip = new GZipStream(packed, CompressionMode.Decompress))
        using (var output = new MemoryStream())
        {
            unzip.CopyTo(output);
            payload = output.ToArray();
        }
        using (var sha = SHA256.Create())
            if (BitConverter.ToString(sha.ComputeHash(payload)).Replace("-", "") != expectedSha)
                throw new InvalidDataException("Frozen legacy fixture SHA256 differs: " + state);
        return new MemoryStream(payload, false);
    }
}
