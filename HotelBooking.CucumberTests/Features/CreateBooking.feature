Feature: Create Booking

    As a user
    I want to create a booking
    So that I can reserve a room
    
    Background: Bookings
        Given the occupied period is from "2026-10-03 12:00:00" to "2026-10-04 12:00:00"


    Scenario: Successfully create a booking (1, 2)
        Given I have a booking with start date "<Start Date>", end date "<End Date>", room id <Room ID> and customer id <Customer ID>
        When I create the booking
        Then the booking should be created successfully


    Examples:
        | Start Date          | End Date            | Room ID | Customer ID |  
        | 2026-10-01 12:00:00 | 2026-10-02 12:00:00 | 1       | 1           | 
        | 2026-10-05 12:00:00 | 2026-10-06 12:00:00 | 1       | 1           | 

    Scenario: Fail to create a booking due to being fully occupied in the period (3)
        Given I have a booking with start date "<Start Date>", end date "<End Date>", room id <Room ID> and customer id <Customer ID>
        When I create the booking
        Then the booking creation should fail due to overlap

    Examples:
        | Start Date          | End Date            | Room ID | Customer ID |
        | 2026-10-02 12:00:00 | 2026-10-05 12:00:00 | 1       | 1           |


    Scenario: Fail to create a booking due to end date overlap (4-5)
        Given I have a booking with start date "<Start Date>", end date "<End Date>", room id <Room ID> and customer id <Customer ID>
        When I create the booking
        Then the booking creation should fail due to overlap
    
    Examples:
        | Start Date          | End Date            | Room ID | Customer ID |
        | 2026-10-02 12:00:00 | 2026-10-03 12:00:00 | 1       | 1           |
        | 2026-10-01 12:00:00 | 2026-10-04 12:00:00 | 1       | 1           |


    Scenario: Fail to create a booking due to start date overlap (6-7)
        Given I have a booking with start date "<Start Date>", end date "<End Date>", room id <Room ID> and customer id <Customer ID>
        When I create the booking
        Then the booking creation should fail due to overlap
    Examples:
        | Start Date          | End Date            | Room ID | Customer ID |
        | 2026-10-03 12:00:00 | 2026-10-05 12:00:00 | 1       | 1           |
        | 2026-10-04 12:00:00 | 2026-10-05 12:00:00 | 1       | 1           |

    Scenario: Fail to create a booking due both start and end date overlap (8-10)
        Given I have a booking with start date "<Start Date>", end date "<End Date>", room id <Room ID> and customer id <Customer ID>
        When I create the booking
        Then the booking creation should fail due to overlap

    Examples:
        | Start Date          | End Date            | Room ID | Customer ID |
        | 2026-10-03 12:00:00 | 2026-10-03 12:00:00 | 1       | 1           |
        | 2026-10-04 12:00:00 | 2026-10-04 12:00:00 | 1       | 1           |
        | 2026-10-03 12:00:00 | 2026-10-04 12:00:00 | 1       | 1           |