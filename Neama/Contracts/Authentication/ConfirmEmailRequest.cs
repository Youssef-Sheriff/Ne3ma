﻿namespace Ne3ma.Contracts.Authentication;

public record ConfirmEmailRequest(
    string UserId,
    string Code
);

