# Decision table-based testing

It is assumed that there is a single range of fully occupied dates (i.e. all rooms are booked during this period). This range is represented by an equivalence class denoted occupied (O).

Dates before the fully occupied range is represented by an equivalence class denoted before (B).

Dates after the fully occupied range is represented by an equivalence class denoted after (A).

We can then use an extended entry decision table to derive testcases.

Startdate is denoted SD.

Enddate is denoted ED.

| Attribute | 1 | 2 | 3 | 4-5 | 6-7 | 8-10 |
|-----------|---|:--|---|-----|-----|------|
| SD in     | B | A | B |  B  |  A  |   O  |
| ED in     | B | A | A |  O  |  O  |   -  |
| Book room | y | y | n |  n  |  n  |   n  |
