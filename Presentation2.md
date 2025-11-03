# Compulsory Presentation 2

| Testcase no. |  1  |  2  |  3  |  4-5 |  6-7 |  8-10 |
|--------------|-----|-----|-----|------|------|-------|
| SD in        |  B  |  A  |  B  |  B   |  O   |  O    |
| ED in        |  B  |  A  |  A  |  O   |  A   |  O    |
| Book room    |  y  |  y  |  n  |  n   |  n   |  n    |

## Test cases with Boundary Value Analysis

### Test 1 (B / B → allowed)

Both SD and ED are before the fully-occupied range. Pick boundary days in B (e.g. the day immediately before the occupied range).

### Test 2 (A / A → allowed)

Both SD and ED are after the fully-occupied range. Pick boundary days in A (e.g. the day immediately after the occupied range).

### Test 3 (B / A → rejected)

SD before and ED after the occupied range (span covers the occupied range). Use boundary days: SD = day before occupied start, ED = day after occupied end.

### Tests 4–5 (B / O → rejected)

SD before, ED inside occupied. Two boundary variants:
ED = first day of occupied (SD = day before occupied start).
ED = last day of occupied (SD = day before occupied start).

### Tests 6–7 (O / A → rejected)

SD inside occupied, ED after. Two boundary variants:
SD = first day of occupied (ED = day after occupied end).
SD = last day of occupied (ED = day after occupied end).

### Tests 8–10 (O / O → rejected)

Both SD and ED inside the occupied range. Use boundary combinations (three useful cases):
SD = first day of occupied, ED = first day of occupied (same-day).
SD = first day of occupied, ED = last day of occupied.
SD = last day of occupied, ED = last day of occupied.

## Functional testing with Cucumber

[Feature File](/HotelBooking.CucumberTests/Features/CreateBooking.feature)

[Step Definitions](/HotelBooking.CucumberTests/StepDefinitions/CreateBookingStepDefinitions.cs)

## API Testing

Api testing is done using the built-in testing framework in ASP.NET Core

The API is run and tested via Web App Factory

[API Test Class](/HotelBooking.IntegrationTests/CreateBookingTests.cs)