Feature: Performance Metrics

  Scenario: Check CPU usage
    Given the Task Manager is running
    When I request the CPU usage
    Then the value should be between 0 and 100