Feature: Create Booking

    As a user
    I want to create a booking
    So that I can reserve a room

    Scenario: Successfully create a booking
        Given I have a booking with start date "<Start Date>", end date "<End Date>", room id <Room ID> and customer id <Customer ID>
        When I create the booking
        Then the booking should be created successfully


    Examples:
        | Start Date          | End Date            | Room ID | Customer ID |
        | 2026-10-01 12:00:00 | 2026-10-05 12:00:00 | 1       | 1           |
