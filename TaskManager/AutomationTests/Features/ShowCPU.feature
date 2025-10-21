Feature: Show CPU
    Verify that CPU usage is displayed correctly in the CPU tab.

    Scenario: CPU usage should be between 0 and 100
        Given the Task Manager is open
        When I navigate to the Performance tab and select CPU
        Then the displayed CPU usage should be between 0 and 100
