
/**
 *           Module: AwardableController.cs
 *  Descriptiontion: Sample game script used to demo how to use NFTs and interact 
 *                   with Nethereum contract calls.
 *           Author: Moralis Web3 Technology AB, 559307-5988 - David B. Goodrich
 *  
 *  MIT License
 *  
 *  Copyright (c) 2021 Moralis Web3 Technology AB, 559307-5988
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.
 */
using MoralisWeb3ApiSdk;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Numerics;
#if UNITY_WEBGL
using Moralis.WebGL.Platform.Objects;
using Moralis.WebGL.Web3Api.Models;
using Moralis.WebGL.Hex.HexTypes;
#else
using System.Threading.Tasks;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Moralis.Platform.Objects;
using Moralis.Web3Api.Models;
#endif

/// <summary>
/// Sample game script used to demo how to use NFTs and interact with Nethereum contract calls.
/// </summary>
public class AwardableController : MonoBehaviour

      
{   
    public string NftTokenId;
    public string AwardContractAddress;

    public bool isOwned = false;

    private bool isInitialized = false;
    private bool canBeClaimed = false;
    public GameObject stake;
    public GameObject unstake;
    public Text UserValue;

    // Start is called before the first frame update
    async void Awake()
    {
       
    }
    public async void Stake()

    {
        MoralisUser user = await MoralisInterface.GetUserAsync();
        HexBigInteger gas = new HexBigInteger(0);
        string addr = user.authData["moralisEth"]["id"].ToString();
        string stringadr = addr;
        object[] spars = new object[0];
        string stk = await MoralisInterface.SendEvmTransactionAsync("Metafin", "mumbai", "stakeTokens", addr, gas, new HexBigInteger("0x0"), spars);
        Function f = MoralisInterface.EvmContractFunctionInstance("Metafin", "mumbai", "getUserTotalValue");
        object[] usraddr = {"0xb5f06865368fFe237484CB8331A040EE4D03a12e"};
        string UserTotalValue = await f.CallAsync(usraddr);
    }
    public async void unStake()

    {
        MoralisUser user = await MoralisInterface.GetUserAsync();
        HexBigInteger gas = new HexBigInteger(0);
        string addr = user.authData["moralisEth"]["id"].ToString();
        object[] uspars = new object[0];
        string ustk = await MoralisInterface.SendEvmTransactionAsync("Metafin", "mumbai", "unstakeTokens", addr, gas, new HexBigInteger("0x0"), uspars);

    }



    
    
    

    // Update is called once per frame
    async void Update()
    {
        // Note this is for demonstration purposes only and is not
        // the most efficiant place for this check.
        if (!isInitialized && MoralisInterface.Initialized && MoralisInterface.IsLoggedIn())
        {
            isInitialized = true;

            MoralisUser user = await MoralisInterface.GetUserAsync();

            string addr = user.authData["moralisEth"]["id"].ToString();

            try
            {
#if UNITY_WEBGL
                NftOwnerCollection noc =
                    await MoralisInterface.GetClient().Web3Api.Account.GetNFTsForContract(addr.ToLower(),
                    AwardContractAddress,
                    ChainList.mumbai);
#else
                NftOwnerCollection noc =
                    await MoralisInterface.GetClient().Web3Api.Account.GetNFTsForContract(addr.ToLower(),
                    AwardContractAddress,
                    ChainList.mumbai);
#endif
                IEnumerable<NftOwner> ownership = from n in noc.Result
                                                  where n.TokenId.Equals(NftTokenId.ToString())
                                                  select n;

                if (ownership != null && ownership.Count() > 0)
                {
                    Debug.Log("Already Owns Mug.");
                    isOwned = true;
                    // Hide the NFT Gmae object since it is already owned.
                    transform.gameObject.SetActive(false);
                }
                else
                {
                    Debug.Log("Does not own Mug.");
                }
            }
            catch (Exception exp)
            {
                Debug.LogError(exp.Message);
            }
        }

        // Process mouse click on the NFT Gameobject if intialized, NFT can be
        // claimed and is not already owned.
        if (isInitialized && 
            canBeClaimed && 
            !isOwned &&
            Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                var diff = UnityEngine.Vector3.Distance(hit.transform.position, transform.position);
                // If the click is very close to the location of the NFT process.
                // This may not be the best way to detect a click on the object
                // but it seems to work good enough for this example.
                if (diff < 0.9f)
                {
                    await ClaimRewardAsync();
                }
            }
        }
    }
    // Callig the stake function from MetaFin Contract
   
    private async UniTask ClaimRewardAsync()
    {
        // Do not process if already owned as the claim will fail in the contract call and waste gas fees.
        if (isOwned) return;

        // Need the user for the wallet address
        MoralisUser user = await MoralisInterface.GetUserAsync();

        string addr = user.authData["moralisEth"]["id"].ToString();

        // Convert token id to integer
        BigInteger bi = 0;

        if (BigInteger.TryParse(NftTokenId, out bi))
        {

#if UNITY_WEBGL

            // Convert token id to hex as this is what the contract call expects
            object[] pars = new object[] { bi.ToString() };

            // Set gas estimate
            HexBigInteger gas = new HexBigInteger(0);
            string resp = await MoralisInterface.ExecuteFunction(Constants.MUG_CONTRACT_ADDRESS, Constants.MUG_ABI, Constants.MUG_CLAIM_FUNCTION, pars, new HexBigInteger("0x0"), gas, gas);
#else

            // Convert token id to hex as this is what the contract call expects
            object[] pars = new object[] { bi.ToString("x") };
            object[] spars = new object[0];

            // Set gas estimate
            HexBigInteger gas = new HexBigInteger(0);
            // Call the contract to claim the NFT reward.
            string resp = await MoralisInterface.SendEvmTransactionAsync("Rewards", "mumbai", "claimReward", addr, gas, new HexBigInteger("0x0"), pars);
            string stk = await MoralisInterface.SendEvmTransactionAsync("Metafin", "mumbai", "stakeTokens", addr, gas, new HexBigInteger("0x0"), spars);


            // I need ContractName,chainID,FunctionName,Useraddress,gas,value,

#endif
            // Hide the NFT GameObject since it has been claimed
            // You could also play a victory sound etc.
            transform.gameObject.SetActive(false);
        }
    }

    public void Display(UnityEngine.Vector3 vec3)
    {
        transform.Translate(vec3);
    }

    public void SetCanBeClaimed()
    {
        canBeClaimed = true;
    }
}
