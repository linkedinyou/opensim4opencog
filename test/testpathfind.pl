:-module(testpathfind, [tpf_method/1, tpf/0, tpf/1, tpf1/0,tpf2/0,tpfi/0, makePipe/2, tpfa/0]).

%
% Change this line to cd to cogbot/bin directory on your system
%  the exists_file is so it doesn't do it again when reconsulted
:- exists_file('testpathfind.pl') -> cd('../bin') ; true.

%% add to search paths
assertIfNewRC(Gaf):-catch(call(Gaf),_,fail),!.
assertIfNewRC(Gaf):-asserta(Gaf).

%
%  These lines need adjusted if you've moved this file
%
:- assertIfNewRC(user:file_search_path(library, '.')).
:- assertIfNewRC(user:file_search_path(test, '../test')).
:- assertIfNewRC(user:file_search_path(cogbot, './prolog/simulator')).
:- assertIfNewRC(user:file_search_path(cogbot, './prolog')).
:- assertIfNewRC(user:file_search_path(library, './prolog')).


:- use_module(test(testsupport)).
:-use_module(library(swicli)).
:-use_module(library('simulator/cogrobot')).

:-discontiguous(test_desc/2).
:-discontiguous(test/1).
:-discontiguous(test_desc_redo/2).

:- dynamic(goMethod/1).

% set the method of movement we're testing

%%goMethod('moveto').  % low level turnTo and move
%%goMethod(autopilot). % only use autoilot
goMethod('astargoto').  % only use A*
%%goMethod('follow*').  % do the best case


% using the test method of movement, go to Location
goByMethod(Location) :-
	goMethod(GM),!, % slight ugliness, just want first one
	Goal =.. [GM, Location],
	botapi(Goal).


%%  teleportTo('annies haven/129.044327/128.206070/81.519630/')
%% teleportTo('annies haven/129.044327/128.206070/80')
%% botapi(autopilot('annies haven/127.044327/128.206070/81.519630/')).
teleportTo(StartName):-
        botapi(stopmoving),
        cogrobot:position_to_v3d(StartName,Start),
        cogrobot:vectorAdd(Start,v3d(0,0,0.7),Start2),
        botapi(teleport(Start2)).
        /*
        %%botapi(waitpos(2,Start)),
        cogrobot:distance_to(Start,Dist),
        %% if fallthru floor try to get closer
        (Dist < 3 -> botapi(moveto(Start)) ; ( cogrobot:vectorAdd(Start2,v3d(0,0,1),Start3),botapi(teleport(Start3)) )),!.*/

dbgFmt(F,A):-'format'(user_error,F,A),flush_output.

% convenience method that does the 'normal' thing -
% tp to Start, move using the standard method to
%  move_test(N,10 , start_swim_surface , stop_swim_surface).

move_test(N,Name):-atom_concat('start_',Name,Start),atom_concat('stop_',Name,Stop),move_test(N,10,Start,Stop).

move_test(N, Time , Start , End) :-
        botapi(stopmoving),
	teleportTo(Start),
        botapi('remeshprim dist 50'),
        botcall(['TheSimAvatar','KillPipes'],_),
        name_to_location_ref(End,LocRef),cli_get(LocRef,'GlobalPosition',Loc),
        dbgFmt('starting test ~w~n',[N]),
        start_test(N),
	goByMethod(Loc),
        botapi(waitpos(Time,End)).


% this is just to debug the test framework with
test_desc(easy , 'insanely easy test').
test(N) :-
	N = easy,
        start_test(N),
	writeq('in easy'),nl,
	std_end(N , 5 , 0).



test_desc(clear , 'clear path 10 meters').
test(N) :-
	N = clear,
	start_test(N),
        move_test(N,15,start_test_1,stop_test_1),
        std_end(N , 17 ,1).


test_desc(zero , 'Zero Distance').
test(N) :-
	N = zero,
	start_test(N),
	move_test(N,3 , start_test_2 , stop_test_2),
        std_end(N , 2 , 0).


test_desc(obstacle , 'Go around obstacle').
test(N) :-
	 N = obstacle,
         start_test(N),
	 move_test(N,25 , start_test_3 , stop_test_3),
         std_end(N , 27 , 2).

test_desc(other_side_wall , 'Goal Other Side Of Wall').
test(N) :-
	N = other_side_wall,
	start_test(N),
        move_test(N,25 , start_test_4 , stop_test_4),
        std_end(N , 27 , 1).

test_desc(elev_path , 'On Elevated Path').
test(N) :-
	N = elev_path,
	start_test(N),
	move_test(N,15 ,
		  start_test_5,
		  stop_test_5),
	std_end(N , 17 , 0).

test_desc(ridge , 'On Elevated land Path').
test(N) :-
	N = ridge,
	start_test(N),
	move_test(N,34 , 'start_ridge','stop_ridge'),
	std_end(N , 40 , 0).

test_desc(ahill , 'Arround Elevated hill').
test(N) :-
	N = ahill,
	start_test(N),
	move_test(N,34 ,
		  'start_ahill',
		  'stop_ahill'),
	std_end(N , 40 , 0).


test_desc_redo(swim_surface , 'Swim arround the island').
test(N) :-
	N = swim_surface,
	start_test(N),
	move_test(N,34 ,
		  'start_swim_surface',
		  'stop_swim_surface'),
	std_end(N , 40 , 0).


test_desc(grnd_maze , 'Ground maze simple').
test(N) :-
	N = grnd_maze,
	start_test(N),
	move_test(N,30 ,
		  start_test_8,
		  stop_test_8),
	std_end(N , 34 , 2).

test_desc(island_hop , 'Island hop').
test(N) :-
	N = island_hop,
	start_test(N),
	move_test(N,45 , start_island_hop , stop_island_hop),
	std_end(N , 45 , 2).

test_desc_redo(hill_walk , 'Hill Walk').
test(N) :-
	N = hill_walk,
	start_test(N),
	move_test(N,60 , start_hill_walk , stop_hill_walk),
	std_end(N , 72 , 1).



test_desc_redo(spiral , 'Spiral Tube').
test1(N) :-
	N = spiral,
	start_test(N),
	move_test(N,60,
		  'annies haven/188.477066/142.809982/81.559509/',
		  'annies haven/181.878403/140.768723/101.555061/'),
	std_end(N , 65 , 1).

tpf_method(GoMethod) :-
	retractall(goMethod(_)),
	asserta(goMethod(GoMethod)),
	cli_set('SimAvatarClient' , 'GotoUseTeleportFallback' , '@'(false)),
        test_desc(Name , Desc),
   once((
        dbgFmt('~n~ndoing test: ~q~n',[test_desc(Name , Desc)]),
	doTest(Name , testpathfind:test(Name) , Results),
        numbervars(Results),
	ppTest([name(Name),
		desc(Desc) ,
		results(Results) ,
		option('goMethod ' , GoMethod)]))),
	fail.

tpf_method(_) :- !.

%% runs the test suite on current bot
tpf :-
	member(Method , [astargoto /* 'follow*' astargoto*/]),
	tpf_method(Method),
	fail.

tpf :- !.

%% runs the test suite on all logged in bots in their own threads
tpfa:-botID(Name,ID),not(running_tpfa(Name)),thread_create(tpfa(Name,ID),_,[detached(true),alias(Name)]),sleep(25),fail.
tpfa:-!.

:-dynamic running_tpfa/1.

tpfa(Name,_ID):-running_tpfa(Name),!.
tpfa(Name,ID):-assert(running_tpfa(Name)),!,set_current_bot(ID),tpf,retractall(running_tpfa(Name)).

tpfi :- tpf(island_hop).
tpf1 :- repeat,once(tpf(island_hop)),sleep(10),fail.
tpf2 :- repeat,once(tpf),sleep(10),fail.


%% example: ?- tpf(clear).
tpf(Name) :-
        goMethod(GoMethod),
	cli_set('SimAvatarClient' , 'GotoUseTeleportFallback' , '@'(false)),
	test_desc(Name , Desc),
	doTest(Name , testpathfind:test(Name) , Results),
	ppTest([name(Name),
		desc(Desc) ,
		results(Results) ,
		option('goMethod ' , GoMethod)]).


 makePipe(S,E):-position_to_v3(S,v3(SX,SY,SZ)),position_to_v3(E,v3(EX,EY,EZ)),
    sformat(SF,'~w,~w,~w,~w,~w,~w,~w,~w,~w',[255,0,0,SX,SY,SZ,EX,EY,EZ]),
    current_bot(BC),cli_call(BC,talk(SF,100,'Normal'),_).

end_of_file.

 current_bot(X),cli_call(X,talk(hi),V)

