# Note 45

A team name cannot exceed 100 characters. Team.NameMaxLength is 100. CreateTeamCommandValidator applies NotEmpty and MaximumLength(Team.NameMaxLength) to the name. TeamMap maps the name column as required with HasMaxLength(Team.NameMaxLength).
