# Decision table-based testing

It is assumed that there is a single range of fully occupied dates (i.e. all rooms are booked during this period). This range is represented by an equivalence class denoted occupied (O).

Dates before the fully occupied range is represented by an equivalence class denoted before (B).

Dates after the fully occupied range is represented by an equivalence class denoted after (A).

We can then use an extended entry decision table to derive testcases.

Startdate is denoted SD.

Enddate is denoted ED.

Without Boundary Value Analysis:

| Rule |  1  |  2  |  3  |  4  |  5  |  6  |  7  |  8  |  9  |
|------|-----|-----|-----|-----|-----|-----|-----|-----|-----|
| SD Class |  B  |  B  |  B  |  O  |  O  |  O  |  A  |  A  |  A  |
| ED Class |  B  |  O  |  A  |  B  |  O  |  A  |  B  |  O  |  A  |
| Outcome  |  y  |  n  |  n  |  n  |  n  |  n  |  n  |  n  |  n  |

With Boundary Value Analysis:

| Testcase no. |  1  |  2  |  3  |  4-5 |  6-7 |  8-10 |
|--------------|-----|-----|-----|------|------|-------|
| SD in        |  B  |  A  |  B  |  B   |  O   |  O    |
| ED in        |  B  |  A  |  A  |  O   |  A   |  O    |
| Book room    |  y  |  y  |  n  |  n   |  n   |  n    |

Values of SD and ED should be tested at limits for each column in the decision table. For example, in the column where “SD in B” and “ED in O” (testcases 4-5), SD should be the day before the start of the fully occupied range, and ED should be the first day in the fully occupied range (testcase 4) and the last day in the fully occupied range (testcase 5).
